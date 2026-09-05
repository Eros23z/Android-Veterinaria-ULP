<template>
  <comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-esqueleto v-if="cargando" />

    <div v-else-if="productos.length === 0" class="ion-padding ion-text-center">
      <p>No se encontraron productos registrados.</p>
    </div>

    <ion-list v-else>
      <ion-item v-for="item in productos" :key="item.id">
        <ion-label>
          <h2>{{ item.nombre }}</h2>
          <p>{{ item.categoria }} - ${{ item.precio.toLocaleString() }}</p>
        </ion-label>
        <ion-badge v-if="!item.disponible" color="medium" slot="end"
          >Sin stock</ion-badge
        >
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { IonList, IonItem, IonLabel, IonBadge } from "@ionic/vue";
import CompPage from "@/components/estructura/comp_page.vue";
import CompEsqueleto from "@/components/base/comp_esqueleto.vue";
import { obtener_productos } from "@/datos/productos";

const cargando = ref(true);
const productos = ref([]);

async function cargar(event = null) {
  cargando.value = true;
  try {
    const res = await obtener_productos();
    productos.value = res.productos;
  } finally {
    cargando.value = false;
    if (event?.target?.complete) {
      event.target.complete();
    }
  }
}

onMounted(() => cargar());
</script>
