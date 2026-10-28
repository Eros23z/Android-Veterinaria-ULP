<template>
  <div class="esqueleto-contenedor ion-padding-horizontal">
    <div v-for="n in cantidad" :key="n" class="vet-card esqueleto-tarjeta">
      <div class="esqueleto-cuerpo">
        <ion-skeleton-text :animated="true" class="esqueleto-icono"></ion-skeleton-text>
        <div class="esqueleto-info">
          <ion-skeleton-text :animated="true" style="width: 55%; height: 16px; border-radius: 4px;"></ion-skeleton-text>
          <ion-skeleton-text :animated="true" style="width: 35%; height: 12px; margin-top: 6px; border-radius: 4px;"></ion-skeleton-text>
        </div>
        <ion-skeleton-text :animated="true" class="esqueleto-badge"></ion-skeleton-text>
      </div>
    </div>
  </div>
</template>

<script setup>
import { IonSkeletonText } from '@ionic/vue';

defineProps({
  cantidad: {
    type: Number,
    default: 4,
  },
});
</script>

<style scoped>
.esqueleto-contenedor {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 8px;
}

.esqueleto-tarjeta {
  padding: 16px;
  background: var(--ion-card-background, #ffffff);
}

.esqueleto-cuerpo {
  display: flex;
  align-items: center;
  gap: 12px;
}

.esqueleto-icono {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  flex-shrink: 0;
}

.esqueleto-info {
  flex: 1;
}

.esqueleto-badge {
  width: 70px;
  height: 22px;
  border-radius: 20px;
  flex-shrink: 0;
}
</style>
