<template>
  <ion-searchbar 
    :debounce="400" 
    :placeholder="placeholder"
    @ionInput="onInput"
    @ionClear="onClear"
  ></ion-searchbar>
</template>

<script setup>
import { IonSearchbar } from '@ionic/vue';

defineProps({
  placeholder: { type: String, default: 'Buscar...' }
});

const emit = defineEmits(['buscar']);

const onInput = (e) => {
  const valor = e.detail?.value ?? e.target?.value ?? '';
  emit('buscar', valor);
};

const onClear = () => {
  emit('buscar', '');
};
</script>
