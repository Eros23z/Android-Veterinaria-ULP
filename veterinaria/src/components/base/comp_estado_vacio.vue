<template>
  <div class="estado-contenedor ion-padding ion-text-center">
    <div class="icono-envoltorio">
      <ion-icon :icon="pawOutline" class="icono-vacio" />
    </div>

    <h2 class="titulo-vacio">{{ titulo }}</h2>
    <p class="mensaje-vacio">{{ mensaje }}</p>

    <slot name="acciones" />
  </div>
</template>

<script setup>
import { IonIcon } from '@ionic/vue';
import { pawOutline } from 'ionicons/icons';

defineProps({
  titulo: {
    type: String,
    default: 'No hay productos disponibles',
  },
  mensaje: {
    type: String,
    default: 'La consulta al catálogo se completó con éxito, pero no se encontraron productos o servicios registrados.',
  },
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
  border-radius: 16px;
  background: var(--ion-color-step-50, #fbfbfb);
  border: 1px solid var(--ion-color-step-150, #e0e0e0);
}

.icono-envoltorio {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.1);
  margin-bottom: 1rem;
}

.icono-vacio {
  font-size: 38px;
  color: var(--ion-color-primary, #3880ff);
}

.titulo-vacio {
  font-size: 1.25rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: var(--ion-text-color, #1a1a1a);
}

.mensaje-vacio {
  font-size: 0.95rem;
  color: var(--ion-color-medium, #92949c);
  max-width: 320px;
  margin: 0;
  line-height: 1.4;
}
</style>
