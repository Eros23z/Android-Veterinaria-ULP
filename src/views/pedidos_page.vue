<template>
  <comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-esqueleto v-if="cargando" />

    <div v-else-if="pedidos.length === 0" class="ion-padding ion-text-center">
      <p>No hay pedidos registrados.</p>
    </div>

    <ion-list v-else>
      <ion-item v-for="p in pedidos" :key="p.id">
        <ion-label>
          <h2>{{ p.id }} - {{ p.cliente || "Mostrador / Sin cliente" }}</h2>
          <p>{{ p.detalle }}</p>
          <p>${{ p.total.toLocaleString() }} - Pago: {{ p.estado_pago }}</p>
        </ion-label>
        <ion-badge :color="obtenerColorEstado(p.estado)" slot="end">
          {{ p.estado }}
        </ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { IonList, IonItem, IonLabel, IonBadge } from "@ionic/vue";
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import { obtener_pedidos } from "@/datos/pedidos";

const cargando = ref(true);
const pedidos = ref([]);

function obtenerColorEstado(estado) {
  switch (estado) {
    case "En preparación":
      return "warning";
    case "Listo":
      return "success";
    case "Entregado":
      return "tertiary";
    case "Cancelado":
      return "danger";
    default:
      return "medium";
  }
}

async function cargar(event = null) {
  cargando.value = true;
  try {
    const res = await obtener_pedidos();
    pedidos.value = res.pedidos;
  } finally {
    cargando.value = false;
    if (event?.target?.complete) {
      event.target.complete();
    }
  }
}

onMounted(() => cargar());
</script>
