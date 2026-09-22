import { createRouter, createWebHistory } from '@ionic/vue-router';
import main_layout from '@/layouts/main_layout.vue';
import { navegacion } from '@/config/navegacion.js';
import { sesion_store } from '@/stores/sesion_store';

const rutas_app = navegacion.map(item => ({
  path: item.ruta.replace('/app/', ''),
  name: item.id,
  component: item.componente,
}));

const routes = [
  {
    path: '/',
    redirect: '/login',
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/login_page.vue'),
  },
  {
    path: '/app',
    component: main_layout,
    children: [
      {
        path: '',
        redirect: '/app/inicio',
      },
      ...rutas_app,
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/login',
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

router.beforeEach(async (to, from, next) => {
  if (sesion_store.restaurando) {
    await sesion_store.inicializar();
  }

  if (to.path === '/login') {
    if (sesion_store.autenticado) {
      return next('/app/inicio');
    }
    return next();
  }

  if (to.path.startsWith('/app')) {
    if (!sesion_store.autenticado) {
      return next('/login');
    }

    // Comprobar si la ruta exige roles específicos
    const navItem = navegacion.find(n => n.ruta === to.path);
    if (navItem && navItem.roles && !sesion_store.tiene_rol(navItem.roles)) {
      return next('/app/inicio');
    }
  }

  next();
});

export default router;