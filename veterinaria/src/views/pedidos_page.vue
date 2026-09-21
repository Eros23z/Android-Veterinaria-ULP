<template>
  <comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-buscador placeholder="Buscar pedidos..." @buscar="onBuscar" />

    <comp-esqueleto v-if="pedidos_store.cargando && pedidos_store.pedidos.length === 0" />

    <div v-else-if="pedidos_store.pedidos.length === 0" class="ion-padding ion-text-center">
      <p>No hay pedidos registrados.</p>
    </div>

    <comp-lista
      v-else
      :total="pedidos_store.paginacion.total"
      :cantidad_mostrada="pedidos_store.pedidos.length"
      :hay_mas="pedidos_store.paginacion.hay_mas"
      :cargando="pedidos_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <ion-list>
          <ion-item v-for="p in pedidos_store.pedidos" :key="p.id">
            <ion-label>
              <h2>Pedido #{{ p.id }} - {{ p.cliente?.nombre || "Sin cliente" }}</h2>
              <p>Fecha: {{ new Date(p.fecha_pedido).toLocaleDateString() }}</p>
              <p>${{ p.total.toLocaleString() }}</p>
              <p v-if="p.notas" style="font-style: italic; font-size: 0.9em;">{{ p.notas }}</p>
            </ion-label>
            <ion-badge :color="obtenerColorEstado(p.estado)" slot="end">
              {{ formatEstado(p.estado) }}
            </ion-badge>
          </ion-item>
        </ion-list>
      </template>
    </comp-lista>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { IonList, IonItem, IonLabel, IonBadge } from "@ionic/vue";
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import compBuscador from "@/components/base/comp_buscador.vue";
import compLista from "@/components/base/comp_lista.vue";
import { pedidos_store } from "@/stores/pedidos_store";

const terminoBusqueda = ref("");

function formatEstado(estado) {
  if (!estado) return '';
  return estado.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
}

function obtenerColorEstado(estado) {
  switch (estado) {
    case "borrador":
      return "light";
    case "confirmado":
      return "primary";
    case "en_preparacion":
      return "warning";
    case "listo":
      return "success";
    case "entregado":
      return "tertiary";
    case "cerrado":
      return "dark";
    case "cancelado":
      return "danger";
    default:
      return "medium";
  }
}

async function cargar(event = null) {
  pedidos_store.reiniciar_paginacion();
  await pedidos_store.cargar_pedidos({ buscar: terminoBusqueda.value });
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await pedidos_store.cargar_mas_pedidos();
}

function onBuscar(termino) {
  terminoBusqueda.value = termino;
  cargar();
}

onMounted(() => cargar());
</script>
