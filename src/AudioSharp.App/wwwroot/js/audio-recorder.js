window.audioRecorder = (() => {
  const defaultOptions = {
    targetSampleRate: 16000,
    channelCount: 1
  };

  let audioContext = null;
  let processor = null;
  let source = null;
  let stream = null;
  let zeroGain = null;
  let buffers = [];
  let inputSampleRate = 0;
  let isRecording = false;
  let options = { ...defaultOptions };
  let antiforgeryToken = null;

  const getAntiforgeryToken = async () => {
    if (antiforgeryToken) {
      return antiforgeryToken;
    }

    const response = await fetch("/api/antiforgery/token", {
      method: "GET",
      credentials: "same-origin"
    });

    if (!response.ok) {
      throw new Error("Failed to get antiforgery token.");
    }

    const payload = await response.json();
    antiforgeryToken = payload.token;
    return antiforgeryToken;
  };

  const start = async (customOptions = {}) => {
    if (isRecording) {
      return true;
    }

    options = { ...defaultOptions, ...customOptions };

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      throw new Error("Audio capture is not supported in this browser.");
    }

    const cleanupOnError = async () => {
      if (stream) {
        stream.getTracks().forEach((track) => track.stop());
      }

      if (audioContext) {
        await audioContext.close();
      }

      resetState();
    };

    stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        channelCount: options.channelCount,
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
        sampleRate: { ideal: 48000 },
        sampleSize: 16
      }
    });

    const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextCtor) {
      await cleanupOnError();
      throw new Error("AudioContext is not available.");
    }

    audioContext = new AudioContextCtor();
    inputSampleRate = audioContext.sampleRate;

    source = audioContext.createMediaStreamSource(stream);
    buffers = [];

    if (!audioContext.audioWorklet) {
      await cleanupOnError();
      throw new Error("AudioWorklet is not supported in this browser.");
    }

    try {
      await audioContext.audioWorklet.addModule("/js/audio-capture-worklet.js");
      processor = new AudioWorkletNode(audioContext, "audio-capture-processor", {
        numberOfInputs: 1,
        numberOfOutputs: 1,
        channelCount: options.channelCount
      });
      processor.port.onmessage = (event) => {
        if (!isRecording) {
          return;
        }

        const data = event.data;
        if (data instanceof Float32Array) {
          buffers.push(data);
        } else if (data && data.buffer) {
          buffers.push(new Float32Array(data));
        }
      };
      processor.port.postMessage({ command: "start" });
    } catch (error) {
      await cleanupOnError();
      throw error;
    }

    zeroGain = audioContext.createGain();
    zeroGain.gain.value = 0;
    source.connect(processor);
    processor.connect(zeroGain);
    zeroGain.connect(audioContext.destination);

    await audioContext.resume();
    isRecording = true;
    return true;
  };

  const stopInternal = async () => {
    if (!isRecording) {
      return null;
    }

    isRecording = false;

    if (processor) {
      processor.disconnect();
      if (processor.port) {
        processor.port.postMessage({ command: "stop" });
        processor.port.onmessage = null;
      } else {
        processor.onaudioprocess = null;
      }
    }

    if (source) {
      source.disconnect();
    }

    if (zeroGain) {
      zeroGain.disconnect();
    }

    if (stream) {
      stream.getTracks().forEach((track) => track.stop());
    }

    if (audioContext) {
      await audioContext.close();
    }

    const totalLength = buffers.reduce((total, buffer) => total + buffer.length, 0);
    if (totalLength === 0) {
      resetState();
      return null;
    }

    const merged = mergeBuffers(buffers, totalLength);
    const downsampled = downsampleBuffer(merged, inputSampleRate, options.targetSampleRate);
    const pcm16 = floatTo16BitPCM(downsampled);
    const wavBuffer = encodeWav(pcm16, options.targetSampleRate);
    resetState();
    return wavBuffer;
  };

  const stop = async () => {
    const wavBuffer = await stopInternal();
    if (!wavBuffer) {
      return { base64: "", contentType: "" };
    }

    const base64 = arrayBufferToBase64(wavBuffer);
    return { base64, contentType: "audio/wav" };
  };

  const stopAndProcess = async (subjectReference, subjectDisplay) => {
    const wavBuffer = await stopInternal();
    if (!wavBuffer) {
      throw new Error("No audio captured.");
    }

    const formData = new FormData();
    formData.append("audio", new Blob([wavBuffer], { type: "audio/wav" }), "recording.wav");

    if (subjectReference) {
      formData.append("subjectReference", subjectReference);
    }

    if (subjectDisplay) {
      formData.append("subjectDisplay", subjectDisplay);
    }

    const token = await getAntiforgeryToken();
    const response = await fetch("/api/audio/process", {
      method: "POST",
      body: formData,
      headers: {
        "X-CSRF-TOKEN": token
      },
      credentials: "same-origin"
    });

    const responseText = await response.text();
    if (!response.ok) {
      let message = "Audio processing failed.";
      try {
        const problem = JSON.parse(responseText);
        message = problem.detail || problem.title || message;
      } catch {
        message = responseText || message;
      }
      throw new Error(message);
    }

    return JSON.parse(responseText);
  };

  const resetState = () => {
    audioContext = null;
    processor = null;
    source = null;
    stream = null;
    zeroGain = null;
    buffers = [];
    inputSampleRate = 0;
  };

  const mergeBuffers = (bufferList, totalLength) => {
    const result = new Float32Array(totalLength);
    let offset = 0;
    for (const buffer of bufferList) {
      result.set(buffer, offset);
      offset += buffer.length;
    }
    return result;
  };

  const downsampleBuffer = (buffer, inputRate, outputRate) => {
    if (outputRate >= inputRate) {
      return buffer;
    }

    const sampleRateRatio = inputRate / outputRate;
    const newLength = Math.round(buffer.length / sampleRateRatio);
    const result = new Float32Array(newLength);
    let offsetResult = 0;
    let offsetBuffer = 0;

    while (offsetResult < result.length) {
      const nextOffsetBuffer = Math.round((offsetResult + 1) * sampleRateRatio);
      let accum = 0;
      let count = 0;

      for (let i = offsetBuffer; i < nextOffsetBuffer && i < buffer.length; i++) {
        accum += buffer[i];
        count++;
      }

      result[offsetResult] = count > 0 ? accum / count : 0;
      offsetResult++;
      offsetBuffer = nextOffsetBuffer;
    }

    return result;
  };

  const floatTo16BitPCM = (input) => {
    const output = new Int16Array(input.length);
    for (let i = 0; i < input.length; i++) {
      const sample = Math.max(-1, Math.min(1, input[i]));
      output[i] = sample < 0 ? sample * 0x8000 : sample * 0x7fff;
    }
    return output;
  };

  const encodeWav = (samples, sampleRate) => {
    const buffer = new ArrayBuffer(44 + samples.length * 2);
    const view = new DataView(buffer);

    writeString(view, 0, "RIFF");
    view.setUint32(4, 36 + samples.length * 2, true);
    writeString(view, 8, "WAVE");
    writeString(view, 12, "fmt ");
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, 1, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, sampleRate * 2, true);
    view.setUint16(32, 2, true);
    view.setUint16(34, 16, true);
    writeString(view, 36, "data");
    view.setUint32(40, samples.length * 2, true);

    let offset = 44;
    for (let i = 0; i < samples.length; i++) {
      view.setInt16(offset, samples[i], true);
      offset += 2;
    }

    return buffer;
  };

  const writeString = (view, offset, value) => {
    for (let i = 0; i < value.length; i++) {
      view.setUint8(offset + i, value.charCodeAt(i));
    }
  };

  const arrayBufferToBase64 = (buffer) => {
    const bytes = new Uint8Array(buffer);
    const chunkSize = 0x8000;
    let binary = "";

    for (let i = 0; i < bytes.length; i += chunkSize) {
      const chunk = bytes.subarray(i, i + chunkSize);
      binary += String.fromCharCode.apply(null, chunk);
    }

    return btoa(binary);
  };

  return { start, stop, stopAndProcess };
})();
