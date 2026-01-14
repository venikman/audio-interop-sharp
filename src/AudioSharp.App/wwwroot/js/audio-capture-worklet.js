class AudioCaptureProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    this.isRecording = false;

    this.port.onmessage = (event) => {
      if (event.data && event.data.command === "start") {
        this.isRecording = true;
      } else if (event.data && event.data.command === "stop") {
        this.isRecording = false;
      }
    };
  }

  process(inputs) {
    if (!this.isRecording) {
      return true;
    }

    const input = inputs[0];
    if (!input || input.length === 0) {
      return true;
    }

    const channelData = input[0];
    if (!channelData) {
      return true;
    }

    const copy = new Float32Array(channelData.length);
    copy.set(channelData);
    this.port.postMessage(copy, [copy.buffer]);

    return true;
  }
}

registerProcessor("audio-capture-processor", AudioCaptureProcessor);
