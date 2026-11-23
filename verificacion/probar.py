"""Ejecuta CP-01 a CP-12 sobre LabCare y consulta las filas de prueba en MySQL."""

import argparse
import html
import re
import subprocess
from http.cookiejar import CookieJar
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import HTTPCookieProcessor, HTTPRedirectHandler, Request, build_opener
from uuid import uuid4


class SinRedireccion(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("url")
    parser.add_argument("--mysql", default="mysql")
    parser.add_argument("--mysql-options", required=True, help="Archivo de opciones MySQL externo al repositorio.")
    parser.add_argument("--evidencias", type=Path, help="Carpeta opcional para respuestas HTML y matriz.")
    args = parser.parse_args()

    cliente = build_opener(HTTPCookieProcessor(CookieJar()), SinRedireccion())
    matriz = []
    marca = uuid4().hex[:8]
    inicio = "/solicitudes"
    nombre = f"Simulacro {marca}"
    editado = f"Turno {marca}"
    descripcion = "El equipo se apaga durante la práctica."

    def solicitar(ruta, estado=200, datos=None):
        cuerpo = urlencode(datos).encode() if datos is not None else None
        peticion = Request(args.url.rstrip("/") + ruta, data=cuerpo)
        try:
            respuesta = cliente.open(peticion, timeout=15)
        except HTTPError as error:
            respuesta = error
        with respuesta:
            contenido = respuesta.read().decode("utf-8")
            if respuesta.status != estado:
                raise AssertionError(f"{ruta}: se esperaba HTTP {estado}, se recibió {respuesta.status}.")
            return contenido

    def token(ruta=inicio):
        cuerpo = solicitar(ruta)
        match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', cuerpo)
        assert match, f"Falta el token del formulario en {ruta}."
        return html.unescape(match.group(1))

    def guardar(ruta, origen, datos, estado):
        return solicitar(ruta, estado, {"__RequestVerificationToken": token(origen), **datos})

    def sql(consulta):
        resultado = subprocess.run(
            [args.mysql, f"--defaults-file={args.mysql_options}", "--batch",
             "--skip-column-names", "--default-character-set=utf8mb4", "-e", consulta],
            check=True, capture_output=True, text=True, encoding="utf-8",
        )
        return resultado.stdout.strip()

    def aprobado(caso, resultado, respuesta=None):
        matriz.append(f"{caso} APROBADO · {resultado}")
        print(matriz[-1], flush=True)
        if args.evidencias and respuesta is not None:
            args.evidencias.mkdir(parents=True, exist_ok=True)
            sin_token = re.sub(r'<input name="__RequestVerificationToken"[^>]*>', "", respuesta)
            (args.evidencias / f"{caso}.html").write_text(sin_token, encoding="utf-8")

    def datos_nuevos(**cambios):
        datos = {"EquipoId": equipo, "Solicitante": nombre, "Descripcion": descripcion, "Prioridad": "ALTA"}
        datos.update(cambios)
        return {f"Nueva.{clave}": valor for clave, valor in datos.items()}

    equipo = int(sql("SELECT id FROM labcare.equipos WHERE codigo = 'EQ-001' AND estado = 'OPERATIVO'"))
    faltante = sql("SELECT COALESCE(MAX(id), 0) + 1 FROM labcare.equipos")
    cantidad = int(sql("SELECT COUNT(*) FROM labcare.solicitudes"))

    guardar(inicio + "/crear", inicio, datos_nuevos(Solicitante=f"  {nombre}  ", Descripcion=f"  {descripcion}  "), 302)
    fila = sql(
        "SELECT id, codigo_solicitud, estado, activo, descripcion, fecha_registro "
        f"FROM labcare.solicitudes WHERE solicitante = '{nombre}'"
    ).split("\t")
    identificador, codigo, estado, activo, texto, fecha = fila
    assert re.fullmatch(r"SOL-\d{8}-[A-F0-9]{10}", codigo)
    assert (estado, activo, texto) == ("PENDIENTE", "1", descripcion)
    detalle = f"{inicio}/{identificador}"
    listado = solicitar(inicio)
    assert codigo in listado
    aprobado("CP-01", f"{codigo} creado como PENDIENTE con activo=1.", listado)

    invalidos = [
        ("CP-02", {"Solicitante": "  Al  "}, "debe tener entre 3 y 100"),
        ("CP-03", {"Descripcion": "  Corta  "}, "debe tener entre 10 y 2000"),
        ("CP-04", {"Prioridad": "URGENTE"}, "La prioridad debe ser"),
        ("CP-05", {"EquipoId": faltante}, "no est&#xE1; disponible"),
    ]
    for caso, cambios, mensaje in invalidos:
        respuesta = guardar(inicio + "/crear", inicio, datos_nuevos(**cambios), 400)
        assert mensaje in respuesta
        assert int(sql("SELECT COUNT(*) FROM labcare.solicitudes")) == cantidad + 1
        aprobado(caso, f"HTTP 400. {html.unescape(mensaje)}. Sin inserción.", respuesta)

    editar = detalle + "/editar"
    assert "Editar solicitud" in solicitar(editar)
    guardar(editar, editar, {
        "Solicitante": editado, "Descripcion": descripcion, "Prioridad": "MEDIA",
        "Id": "0", "CodigoSolicitud": "FALSO", "Estado": "CANCELADA",
    }, 302)
    edit = sql(
        "SELECT solicitante, prioridad, codigo_solicitud, estado, fecha_registro, equipo_id "
        f"FROM labcare.solicitudes WHERE id = {identificador}"
    ).split("\t")
    assert edit == [editado, "MEDIA", codigo, "PENDIENTE", fecha, str(equipo)]
    aprobado("CP-06", "Edición guardada. ID, código, fecha, equipo y estado conservados.", solicitar(detalle))

    guardar(detalle + "/estado", detalle, {"destino": "EN_PROCESO"}, 302)
    assert sql(f"SELECT estado FROM labcare.solicitudes WHERE id = {identificador}") == "EN_PROCESO"
    aprobado("CP-07", "PENDIENTE → EN_PROCESO guardado en MySQL.", solicitar(detalle))

    guardar(detalle + "/estado", detalle, {"destino": "RESUELTA"}, 302)
    respuesta = guardar(detalle + "/estado", inicio, {"destino": "PENDIENTE"}, 409)
    assert "no est&#xE1; permitida" in respuesta
    assert sql(f"SELECT estado FROM labcare.solicitudes WHERE id = {identificador}") == "RESUELTA"
    aprobado("CP-08", "HTTP 409. RESUELTA → PENDIENTE rechazada y estado conservado.", respuesta)

    def buscar(**filtros):
        return solicitar(inicio + "?" + urlencode(filtros))

    assert codigo in buscar(q=editado.upper(), prioridad="MEDIA", estado="RESUELTA")
    assert codigo in buscar(q="COMPUTADORA DE ESCRITORIO", estado="RESUELTA")
    assert codigo in buscar(q=codigo.lower())
    assert codigo not in buscar(q=editado, prioridad="BAJA")
    assert codigo not in buscar(q=editado, estado="EN_PROCESO")
    aprobado("CP-10", "Coincidencia por código, solicitante y equipo. Prioridad y estado restringen el resultado.",
             buscar(q=editado.upper(), prioridad="MEDIA", estado="RESUELTA"))

    id_inexistente = sql("SELECT MAX(id) + 1 FROM labcare.solicitudes")
    for ruta in (f"{inicio}/{id_inexistente}", f"{inicio}/{id_inexistente}/editar"):
        respuesta = solicitar(ruta, 404)
        assert "No se encontr" in respuesta
    aprobado("CP-11", "Tanto detalle como edición devuelven HTTP 404 para el ID inexistente.", respuesta)

    cliente = build_opener(HTTPCookieProcessor(CookieJar()), SinRedireccion())
    recargado = buscar(q=codigo)
    assert codigo in recargado and editado in recargado
    aprobado("CP-12", "El registro reaparece en un cliente nuevo, sin cookies de la sesión original.", recargado)

    eliminar = detalle + "/eliminar"
    assert "Confirmar baja" in solicitar(eliminar)
    guardar(eliminar, eliminar, {}, 302)
    listado = buscar(q=codigo)
    tabla = listado.partition("<table")[2].partition("</table>")[0]
    assert codigo not in tabla
    assert sql(f"SELECT activo FROM labcare.solicitudes WHERE id = {identificador}") == "0"
    solicitar(detalle, 404)
    aprobado("CP-09", "El código desapareció del listado. La fila en MySQL conserva activo=0.", listado)

    if args.evidencias:
        (args.evidencias / "resultados.txt").write_text("\n".join(sorted(matriz)) + "\n", encoding="utf-8")
        (args.evidencias / "persistencia.txt").write_text(
            "id\tcodigo_solicitud\testado\tactivo\n" + sql(
                "SELECT id, codigo_solicitud, estado, activo FROM labcare.solicitudes "
                f"WHERE id = {identificador}"
            ) + "\n", encoding="utf-8",
        )


if __name__ == "__main__":
    main()
