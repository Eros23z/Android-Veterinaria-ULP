<template>
  <div class="estado-contenedor ion-padding ion-text-center">
    <div class="icono-envoltorio">
      <ion-icon :icon="cloudOfflineOutline" class="icono-alerta" />
    </div>

    <h2 class="titulo-error">{{ titulo }}</h2>
    <p class="mensaje-error">{{ mensaje }}</p>

    <div class="acciones">
      <ion-button
        color="danger"
        fill="outline"
        shape="round"
        @click="$emit('reintentar')"
      >
        <ion-icon slot="start" :icon="refreshOutline"></ion-icon>
        Reintentar
      </ion-button>
    </div>
  </div>
</template>

<script setup>
import { IonButton, IonIcon } from '@ionic/vue';
import { cloudOfflineOutline, refreshOutline } from 'ionicons/icons';

defineProps({
  titulo: {
    type: String,
    default: 'Error de Conexión',
  },
  mensaje: {
    type: String,
    default: 'No se pudo conectar con el servidor. Verifique su red y asegúrese de que el backend esté en ejecución.',
  },
});

defineEmits(['reintentar']);
</script>

<style scoped>
.estado-contenedor {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2.5rem 1.5rem;
  margin: 1.5rem 1rem;
  border-radius: 16px;
  background: var(--ion-color-step-50, #fbfbfb);
  border: 1px dashed var(--ion-color-danger-tint, #ff8182);
}

.icono-envoltorio {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.12);
  margin-bottom: 1rem;
}

.icono-alerta {
  font-size: 38px;
  color: var(--ion-color-danger, #eb445a);
}

.titulo-error {
  font-size: 1.25rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: var(--ion-color-danger-shade, #cf3c4f);
}

.mensaje-error {
  font-size: 0.95rem;
  color: var(--ion-color-medium, #92949c);
  max-width: 320px;
  margin: 0 0 1.5rem 0;
  line-height: 1.4;
}

.acciones ion-button {
  --padding-start: 1.5rem;
  --padding-end: 1.5rem;
  font-weight: 600;
}
</style>
