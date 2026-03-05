export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  modules: ['@nuxt/ui'],
  css: ['~/assets/css/main.css'],
  colorMode: {
    preference: 'light',
    fallback: 'light',
    classSuffix: '',
  },
  app: {
    head: {
      htmlAttrs: { class: 'light' },
    },
  },
  runtimeConfig: {
    public: {
      apiBaseUrl: 'http://localhost:5279',
    },
  },
})