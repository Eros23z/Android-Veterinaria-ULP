<template>
  <div v-if="disponible" class="dictado-boton-contenedor">
    <ion-button
      type="button"
      size="small"
      fill="clear"
      :color="escuchando ? 'danger' : 'primary'"
      class="boton-mic"
      :title="escuchando ? 'Escuchando voz...' : 'Dictar por voz'"
      :disabled="escuchando"
      @click="iniciarDictado"
    >
      <ion-spinner v-if="escuchando" name="crescent" class="spinner-dictado"></ion-spinner>
      <ion-icon v-else slot="icon-only" :icon="micOutline" class="icono-mic"></ion-icon>
    </ion-button>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { IonButton, IonIcon, IonSpinner } from '@ionic/vue';
import { micOutline } from 'ionicons/icons';
import { dictado_service } from '@/services/dictado_service';
import { vibracion_service } from '@/services/vibracion_service';

const props = defineProps({
  valor: {
    type: String,
    default: '',
  },
  idioma: {
    type: String,
    default: 'es-AR',
  },
});

const emit = defineEmits(['actualizar']);

const disponible = ref(false);
const escuchando = ref(false);

async function iniciarDictado() {
  if (escuchando.value) return;

  escuchando.value = true;
  await vibracion_service.vibrar_toque();

  try {
    const res = await dictado_service.escuchar(props.idioma);
    if (res.ok && res.texto && res.texto.trim()) {
      await vibracion_service.vibrar_toque();
      const textoNuevo = res.texto.trim();
      const acumulado = props.valor && props.valor.trim()
        ? `${props.valor.trim()} ${textoNuevo}`
        : textoNuevo;
      emit('actualizar', acumulado);
    } else if (res.mensaje && !res.mensaje.includes('cancelado')) {
      await vibracion_service.vibrar_error();
    }
  } catch (error) {
    console.warn('Error en dictado:', error);
    await vibracion_service.vibrar_error();
  } finally {
    escuchando.value = false;
  }
}

onMounted(async () => {
  disponible.value = await dictado_service.dictado_disponible();
});
</script>

<style scoped>
.dictado-boton-contenedor {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.boton-mic {
  --padding-start: 6px;
  --padding-end: 6px;
  margin: 0;
  height: 36px;
  width: 36px;
  border-radius: 50%;
  transition: transform 0.2s ease;
}

.boton-mic:active {
  transform: scale(0.92);
}

.icono-mic {
  font-size: 1.35rem;
}

.spinner-dictado {
  width: 18px;
  height: 18px;
}
</style>
