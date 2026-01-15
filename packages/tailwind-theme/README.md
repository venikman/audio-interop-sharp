# Tailwind Theme (AudioSharp)

Reusable Tailwind v4 theme tokens and base styling. Includes shadcn-compatible CSS variables for easy parity with shadcn/lyra.

## Usage (local repo)

In your Tailwind entry CSS:

```css
@import "../packages/tailwind-theme/fonts.css";
@import "tailwindcss";
@import "../packages/tailwind-theme/theme.css";
```

## Usage (npm package)

1) Publish this package or use `npm link`.
2) Import after Tailwind:

```css
@import "@audiosharp/tailwind-theme/fonts.css";
@import "tailwindcss";
@import "@audiosharp/tailwind-theme/theme.css";
```

## Notes

- Fonts are loaded from Google Fonts in `fonts.css` (Noto Sans). Skip that import if you already load fonts.
- shadcn tokens are defined on `:root` and `.dark`.
- `--radius` is set to `0px` to match the requested preset; override if you want rounded components.
