const RETARDO_SIMULADO = 600;

const PEDIDOS = [
  { id: 'PED-0001', cliente: 'Marta Quiroga', detalle: 'Vacuna Antirrábica + Control', total: 20500, estado: 'En preparación', estado_pago: 'Pendiente' },
  { id: 'PED-0002', cliente: 'Jorge Lucero', detalle: 'Pipeta Antipulgas x2', total: 14400, estado: 'Listo', estado_pago: 'Pagado' },
  { id: 'PED-0003', cliente: null, detalle: 'Alimento Balanceado Mostrador', total: 45000, estado: 'Entregado', estado_pago: 'Pagado' }, // Caso incompleto
  { id: 'PED-0004', cliente: 'Ana Sosa', detalle: 'Ecografía Abdominal', total: 18000, estado: 'Cancelado', estado_pago: 'Anulado' }
];

export function obtener_pedidos() {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({ pedidos: PEDIDOS });
    }, RETARDO_SIMULADO);
  });
}