<template>
  <div class="estado-contenedor ion-padding ion-text-center">
    <div class="icono-envoltorio" :class="`tipo-${tipoCalculado}`">
      <ion-icon :icon="iconoCalculado" class="icono-vacio" />
    </div>

    <h2 class="titulo-vacio">{{ tituloCalculado }}</h2>
    <p class="mensaje-vacio">{{ mensajeCalculado }}</p>

    <div v-if="textoBoton || $slots.acciones" class="acciones-vacio ion-margin-top">
      <slot name="acciones">
        <ion-button
          v-if="textoBoton"
          shape="round"
          :fill="tipoCalculado === 'filtros' ? 'outline' : 'solid'"
          :color="tipoCalculado === 'sin_red' ? 'warning' : 'primary'"
          @click="$emit('accion')"
        >
          <ion-icon v-if="tipoCalculado === 'filtros'" slot="start" :icon="refreshOutline"></ion-icon>
          {{ textoBoton }}
        </ion-button>
      </slot>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { IonIcon, IonButton } from '@ionic/vue';
import {
  pawOutline,
  searchOutline,
  cloudOfflineOutline,
  refreshOutline,
} from 'ionicons/icons';

const props = defineProps({
  tipo: {
    type: String,
    default: 'general', // 'general' | 'filtros' | 'sin_red'
  },
  titulo: {
    type: String,
    default: '',
  },
  mensaje: {
    type: String,
    default: '',
  },
  textoBoton: {
    type: String,
    default: '',
  },
});

defineEmits(['accion']);

const tipoCalculado = computed(() => props.tipo || 'general');

const iconoCalculado = computed(() => {
  switch (tipoCalculado.value) {
    case 'sin_red':
      return cloudOfflineOutline;
    case 'filtros':
      return searchOutline;
    default:
      return pawOutline;
  }
});

const tituloCalculado = computed(() => {
  if (props.titulo) return props.titulo;
  switch (tipoCalculado.value) {
    case 'sin_red':
      return 'Sin conexión a internet';
    case 'filtros':
      return 'No hay resultados para tu búsqueda';
    default:
      return 'No hay registros disponibles';
  }
});

const mensajeCalculado = computed(() => {
  if (props.mensaje) return props.mensaje;
  switch (tipoCalculado.value) {
    case 'sin_red':
      return 'No pudimos conectar con el servidor. Puedes seguir viendo los datos guardados en tu dispositivo.';
    case 'filtros':
      return 'Probá cambiando los términos de búsqueda o limpiando los filtros seleccionados.';
    default:
      return 'Aún no se han generado registros en esta sección. Podés crear uno nuevo con el botón de abajo.';
  }
});
</script>

<style scoped>
.estado-contenedor {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1.5rem;
  margin: 1.5rem 1rem;
  border-radius: var(--vet-radio, 16px);
  background: var(--ion-color-step-50, #fbfbfb);
  border: var(--vet-border, 1px solid #e2e8f0);
}

.icono-envoltorio {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 76px;
  height: 76px;
  border-radius: 50%;
  margin-bottom: 1rem;
}

.icono-envoltorio.tipo-general {
  background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.12);
  color: var(--ion-color-primary, #3880ff);
}

.icono-envoltorio.tipo-filtros {
  background: rgba(var(--ion-color-medium-rgb, 146, 148, 156), 0.14);
  color: var(--ion-color-step-700, #475569);
}

.icono-envoltorio.tipo-sin_red {
  background: rgba(var(--ion-color-warning-rgb, 255, 196, 9), 0.18);
  color: var(--ion-color-warning-shade, #b88600);
}

.icono-vacio {
  font-size: 38px;
}

.titulo-vacio {
  font-size: 1.25rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: var(--ion-text-color, #1a1a1a);
}

.mensaje-vacio {
  font-size: 0.95rem;
  color: var(--vet-muted, #565a66);
  max-width: 320px;
  margin: 0;
  line-height: 1.4;
}

.acciones-vacio {
  margin-top: 1.25rem;
}
</style>
