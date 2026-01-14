# audio-interop-sharp

## Frontend styling

- Build Tailwind CSS: `npm run build:css`
- Watch Tailwind CSS: `npm run watch:css`
- `dotnet publish` runs `npm run build:css`, so Node + npm are required.
- Skip Tailwind during publish: `dotnet publish -p:SkipTailwindCss=true`
