<template>
  <div class="indicadores-scroll">
    <div class="indicadores-contenedor">
      <button
        v-for="item in indicadores"
        :key="item.id"
        type="button"
        class="chip-indicador"
        :class="{ 'chip-activo': item.id === seleccionado }"
        @click="alSeleccionar(item.id)"
      >
        <span class="chip-etiqueta">{{ item.etiqueta }}</span>
        <span
          class="chip-conteo"
          :class="`conteo-${item.color || 'medium'}`"
        >
          {{ item.cantidad ?? 0 }}
        </span>
      </button>
    </div>
  </div>
</template>

<script setup>
import { vibracion_service } from '@/services/vibracion_service';

const props = defineProps({
  indicadores: {
    type: Array,
    required: true,
    default: () => [],
  },
  seleccionado: {
    type: [String, Number, null],
    default: null,
  },
});

const emit = defineEmits(['seleccionar']);

async function alSeleccionar(id) {
  await vibracion_service.vibrar_toque();
  emit('seleccionar', id);
}
</script>

<style scoped>
.indicadores-scroll {
  width: 100%;
  overflow-x: auto;
  scrollbar-width: none;
  -webkit-overflow-scrolling: touch;
  padding: 4px 16px 8px;
}

.indicadores-scroll::-webkit-scrollbar {
  display: none;
}

.indicadores-contenedor {
  display: flex;
  gap: 8px;
  align-items: center;
}

.chip-indicador {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--ion-color-step-200, #e2e8f0);
  background: var(--ion-color-step-50, #f8fafc);
  color: var(--ion-color-step-800, #334155);
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.2s ease;
}

.chip-indicador:active {
  transform: scale(0.96);
}

.chip-activo {
  background: var(--ion-color-primary, #3880ff);
  color: #ffffff;
  border-color: var(--ion-color-primary, #3880ff);
  box-shadow: 0 4px 12px rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.3);
}

.chip-conteo {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  border-radius: 10px;
  font-size: 0.75rem;
  font-weight: 700;
  background: var(--ion-color-step-200, #cbd5e1);
  color: var(--ion-color-step-900, #0f172a);
}

.chip-activo .chip-conteo {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

.conteo-primary { background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.2); color: var(--ion-color-primary, #3880ff); }
.conteo-success { background: rgba(var(--ion-color-success-rgb, 45, 211, 111), 0.2); color: var(--ion-color-success-shade, #1ea753); }
.conteo-warning { background: rgba(var(--ion-color-warning-rgb, 255, 196, 9), 0.25); color: var(--ion-color-warning-shade, #b88600); }
.conteo-danger  { background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.2); color: var(--ion-color-danger, #eb445a); }
</style>
