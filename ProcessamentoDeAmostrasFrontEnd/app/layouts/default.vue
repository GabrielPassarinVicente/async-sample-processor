<template>
  <div class="min-h-screen flex flex-col bg-slate-50">
    <header class="bg-white border-b border-slate-200 sticky top-0 z-50 shadow-sm">
      <div class="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-xl bg-primary-600 flex items-center justify-center shadow-sm">
            <UIcon name="i-heroicons-signal" class="text-white text-lg" />
          </div>
          <div>
            <span class="font-extrabold text-slate-900 text-lg tracking-tight">Sensores Ambientais</span>
            <span class="ml-2 text-xs font-semibold text-primary-600 bg-primary-50 px-2 py-0.5 rounded-full border border-primary-100">Processamento Assíncrono de Amostras</span>
          </div>
        </div>
        <div class="hidden sm:flex items-center gap-2">
          <span class="w-2 h-2 rounded-full bg-green-500 inline-block animate-pulse"></span>
          <span class="text-sm text-slate-500 font-medium">Sistema ativo</span>
        </div>
      </div>
    </header>

    <main class="flex-1 w-full max-w-7xl mx-auto px-4 sm:px-6 py-8">
      <slot />
    </main>
  </div>
</template>
