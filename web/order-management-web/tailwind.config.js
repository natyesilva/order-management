/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        ink: {
          50: '#f5f7fb',
          100: '#e9eef7',
          200: '#cfdcf0',
          700: '#20314a',
          900: '#0b1220',
        },
      },
    },
  },
  plugins: [],
}

