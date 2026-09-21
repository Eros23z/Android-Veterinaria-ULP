<template>
  <div class="comp-lista">
    <slot name="lista"></slot>
    <div class="paginacion-info ion-padding" v-if="total > 0">
      <p>Mostrando {{ cantidad_mostrada }} de {{ total }}</p>
      <ion-button v-if="hay_mas" expand="block" fill="outline" @click="$emit('ver_mas')" :disabled="cargando">
        {{ cargando ? 'Cargando...' : 'Ver más' }}
      </ion-button>
    </div>
  </div>
</template>

<script setup>
import { IonButton } from '@ionic/vue';

defineProps({
  total: { type: Number, required: true },
  cantidad_mostrada: { type: Number, required: true },
  hay_mas: { type: Boolean, required: true },
  cargando: { type: Boolean, default: false }
});

defineEmits(['ver_mas']);
</script>
<style scoped>
.paginacion-info {
  text-align: center;
}
</style>
