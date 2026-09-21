export const armarConsulta = (parametros) => {
    if (!parametros) return '';
    const query = new URLSearchParams();
    for (const clave in parametros) {
        if (parametros[clave] !== null && parametros[clave] !== undefined && parametros[clave] !== '') {
            query.append(clave, parametros[clave]);
        }
    }
    const queryString = query.toString();
    return queryString ? `?${queryString}` : '';
};
