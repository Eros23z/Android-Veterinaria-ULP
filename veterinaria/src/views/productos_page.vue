<template>
  <comp-page
    titulo="Productos"
    :mostrar_actualizar="true"
    @actualizar="actualizarCatalogo"
  >
    <!-- 1. Estado Cargando -->
    <comp-esqueleto v-if="productos_store.cargando" />

    <!-- 2. Estado Error -->
    <comp-estado-error
      v-else-if="productos_store.error"
      :mensaje="productos_store.error"
      @reintentar="reintentarCarga"
    />

    <!-- 3. Estado Vacío -->
    <comp-estado-vacio
      v-else-if="productos_store.productos.length === 0"
    />

    <!-- 4. Estado Lista de Productos -->
    <ion-list v-else>
      <ion-item
        v-for="item in productos_store.productos"
        :key="item.id"
        lines="inset"
        class="item-producto"
      >
        <ion-thumbnail slot="start" class="miniatura-producto">
          <img :src="resolverUrlImagen(item.imagen_url)" :alt="item.nombre" />
        </ion-thumbnail>

        <ion-label>
          <h2 class="nombre-producto">{{ item.nombre }}</h2>
          <p class="categoria-producto">{{ item.categoria }}</p>
          <p class="precio-producto">
            ${{ Number(item.precio).toLocaleString('es-AR') }}
          </p>
        </ion-label>

        <ion-badge
          v-if="!item.disponible"
          color="medium"
          slot="end"
          class="badge-estado"
        >
          Sin stock
        </ion-badge>
        <ion-badge
          v-else
          color="success"
          slot="end"
          class="badge-estado"
        >
          Disponible
        </ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup>
import { onMounted } from 'vue';
import {
  IonList,
  IonItem,
  IonLabel,
  IonBadge,
  IonThumbnail,
} from '@ionic/vue';
import CompPage from '@/components/estructura/comp_page.vue';
import CompEsqueleto from '@/components/base/comp_esqueleto.vue';
import CompEstadoError from '@/components/base/comp_estado_error.vue';
import CompEstadoVacio from '@/components/base/comp_estado_vacio.vue';
import { productos_store } from '@/stores/productos_store';
import { obtener_api_url } from '@/config/debug';

function resolverUrlImagen(imagenUrl) {
  if (!imagenUrl) {
    return `${obtener_api_url()}/seed/consulta.svg`;
  }
  if (imagenUrl.startsWith('http://') || imagenUrl.startsWith('https://')) {
    return imagenUrl;
  }
  const base = obtener_api_url();
  return `${base}${imagenUrl.startsWith('/') ? '' : '/'}${imagenUrl}`;
}

async function actualizarCatalogo(event) {
  try {
    await productos_store.cargar(true);
  } finally {
    if (event?.target?.complete) {
      event.target.complete();
    }
  }
}

function reintentarCarga() {
  productos_store.cargar(true);
}

onMounted(() => {
  if (productos_store.productos.length === 0) {
    productos_store.cargar();
  }
});
</script>

<style scoped>
.item-producto {
  --padding-top: 10px;
  --padding-bottom: 10px;
}

.miniatura-producto {
  --size: 48px;
  --border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--ion-color-step-100, #f0f0f0);
  padding: 4px;
}

.miniatura-producto img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.nombre-producto {
  font-weight: 600;
  font-size: 1rem;
  margin-bottom: 2px;
}

.categoria-producto {
  font-size: 0.85rem;
  color: var(--ion-color-medium, #92949c);
  margin-bottom: 4px;
}

.precio-producto {
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--ion-color-primary, #3880ff);
  margin: 0;
}

.badge-estado {
  font-size: 0.75rem;
  padding: 4px 8px;
  border-radius: 6px;
}
</style>
