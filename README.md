# audio-interop-sharp

[![Cloud Run](https://img.shields.io/badge/Cloud%20Run-deployed-0f9d58?logo=googlecloud&logoColor=white)](https://audiosharp-3xq2dkzopa-uc.a.run.app)

## Frontend styling

- Build Tailwind CSS: `npm run build:css`
- Watch Tailwind CSS: `npm run watch:css`
- `dotnet publish` runs `npm run build:css`, so Node + npm are required.
- Skip Tailwind during publish: `dotnet publish -p:SkipTailwindCss=true`
