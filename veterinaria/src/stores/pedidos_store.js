import { reactive } from 'vue';
import { enviar } from '../services/ajax_service';
import { armarConsulta } from '../utils/consulta';

export const pedidos_store = reactive({
    pedidos: [],
    pedido_actual: null,
    paginacion: { pagina: 1, tamano: 10, total: 0, hay_mas: false },
    busqueda: '',
    estado_filtro: null,
    cargando: false,
    error: null,

    async cargar_pedidos(parametros = {}) {
        this.cargando = true;
        this.error = null;
        try {
            const params = {
                pagina: this.paginacion.pagina,
                tamano: this.paginacion.tamano,
                ...(this.busqueda ? { busqueda: this.busqueda } : {}),
                ...(this.estado_filtro ? { estado: this.estado_filtro } : {}),
                ...parametros
            };
            const query = armarConsulta(params);
            const respuesta = await enviar(`/api/pedidos${query}`);
            
            const items = respuesta.pedidos ?? respuesta.items ?? [];
            const pag = (respuesta.pagina && typeof respuesta.pagina === 'object') ? respuesta.pagina : respuesta;

            if (this.paginacion.pagina === 1) {
                this.pedidos = items;
            } else {
                this.pedidos = [...this.pedidos, ...items];
            }
            
            this.paginacion = {
                pagina: pag.pagina ?? 1,
                tamano: pag.tamano ?? 10,
                total: pag.total ?? 0,
                hay_mas: pag.hay_mas ?? false
            };
        } catch (error) {
            this.error = error.message;
        } finally {
            this.cargando = false;
        }
    },

    async buscar(texto) {
        this.busqueda = texto || '';
        this.reiniciar_paginacion();
        await this.cargar_pedidos();
    },

    async filtrar_por_estado(estado) {
        this.estado_filtro = estado || null;
        this.reiniciar_paginacion();
        await this.cargar_pedidos();
    },

    async cargar_mas_pedidos() {
        if (this.paginacion.hay_mas && !this.cargando) {
            this.paginacion.pagina++;
            await this.cargar_pedidos();
        }
    },

    async cambiar_estado_pedido(id, nuevo_estado) {
        this.cargando = true;
        this.error = null;
        try {
            await enviar(`/api/pedidos/${id}/estado`, 'PATCH', { estado: nuevo_estado });
            const pedidoIndex = this.pedidos.findIndex(p => p.id === id);
            if (pedidoIndex !== -1) {
                this.pedidos[pedidoIndex].estado = nuevo_estado;
            }
        } catch (error) {
            this.error = error.message;
        } finally {
            this.cargando = false;
        }
    },
    
    reiniciar_paginacion() {
        this.paginacion.pagina = 1;
        this.pedidos = [];
    }
});

