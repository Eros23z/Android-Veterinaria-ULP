<template>
  <comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-esqueleto v-if="cargando" />

    <div v-else-if="clientes.length === 0" class="ion-padding ion-text-center">
      <p>No hay clientes registrados.</p>
    </div>

    <ion-list v-else>
      <ion-item v-for="c in clientes" :key="c.id">
        <ion-label>
          <h2>{{ c.nombre }}</h2>
          <p>Mascota: {{ c.mascota }} | Tel: {{ c.telefono }}</p>
          <p v-if="c.direccion">{{ c.direccion }}</p>
          <p v-else style="color: var(--ion-color-medium); font-style: italic">
            Sin dirección
          </p>
        </ion-label>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { IonList, IonItem, IonLabel } from "@ionic/vue";
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import { obtener_clientes } from "@/datos/clientes";

const cargando = ref(true);
const clientes = ref([]);

async function cargar(event = null) {
  cargando.value = true;
  try {
    const res = await obtener_clientes();
    clientes.value = res.clientes;
  } finally {
    cargando.value = false;
    if (event?.target?.complete) {
      event.target.complete();
    }
  }
}

onMounted(() => cargar());
</script>
