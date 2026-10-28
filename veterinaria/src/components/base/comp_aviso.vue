<template>
  <div class="vet-aviso" :class="`tono-${tonoCalculado}`">
    <div class="aviso-icono-col">
      <ion-icon :icon="iconoCalculado" class="aviso-icono"></ion-icon>
    </div>
    <div class="aviso-contenido">
      <strong v-if="titulo" class="aviso-titulo">{{ titulo }}</strong>
      <p v-if="mensaje" class="aviso-mensaje">{{ mensaje }}</p>
      <slot />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { IonIcon } from '@ionic/vue';
import {
  alertCircleOutline,
  warningOutline,
  timeOutline,
  informationCircleOutline,
} from 'ionicons/icons';

const props = defineProps({
  tono: {
    type: String,
    default: 'advertencia', // 'peligro' | 'advertencia' | 'pendiente' | 'info'
  },
  titulo: {
    type: String,
    default: '',
  },
  mensaje: {
    type: String,
    default: '',
  },
  icono: {
    type: String,
    default: '',
  },
});

const tonoCalculado = computed(() => {
  const t = (props.tono || '').toLowerCase();
  if (t === 'danger' || t === 'error') return 'peligro';
  if (t === 'warning') return 'advertencia';
  if (t === 'pendiente' || t === 'reloj') return 'pendiente';
  if (t === 'info' || t === 'informacion') return 'info';
  return props.tono || 'advertencia';
});

const iconoCalculado = computed(() => {
  if (props.icono) return props.icono;
  switch (tonoCalculado.value) {
    case 'peligro':
      return alertCircleOutline;
    case 'advertencia':
      return warningOutline;
    case 'pendiente':
      return timeOutline;
    case 'info':
    default:
      return informationCircleOutline;
  }
});
</script>

<style scoped>
.vet-aviso {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 14px;
  border-radius: var(--vet-radio-chico, 10px);
  margin-bottom: 12px;
  line-height: 1.35;
}

.aviso-icono-col {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  margin-top: 1px;
}

.aviso-icono {
  font-size: 22px;
}

.aviso-contenido {
  flex: 1;
}

.aviso-titulo {
  display: block;
  font-size: 0.9rem;
  font-weight: 700;
  margin-bottom: 2px;
}

.aviso-mensaje {
  margin: 0;
  font-size: 0.85rem;
}

/* Tonos */
.tono-peligro {
  background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.12);
  border-left: 4px solid var(--ion-color-danger, #eb445a);
  color: var(--ion-color-danger-shade, #b82d3e);
}
.tono-peligro .aviso-mensaje {
  color: var(--ion-color-step-850, #1e293b);
}

.tono-advertencia {
  background: rgba(var(--ion-color-warning-rgb, 255, 196, 9), 0.15);
  border-left: 4px solid var(--ion-color-warning, #ffc409);
  color: var(--ion-color-warning-shade, #856404);
}
.tono-advertencia .aviso-mensaje {
  color: var(--ion-color-step-850, #1e293b);
}

.tono-pendiente {
  background: rgba(var(--ion-color-tertiary-rgb, 82, 96, 255), 0.12);
  border-left: 4px solid var(--ion-color-tertiary, #5260ff);
  color: var(--ion-color-tertiary-shade, #3a48e0);
}
.tono-pendiente .aviso-mensaje {
  color: var(--ion-color-step-850, #1e293b);
}

.tono-info {
  background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.12);
  border-left: 4px solid var(--ion-color-primary, #3880ff);
  color: var(--ion-color-primary-shade, #245dc7);
}
.tono-info .aviso-mensaje {
  color: var(--ion-color-step-850, #1e293b);
}
</style>
