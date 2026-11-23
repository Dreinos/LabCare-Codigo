CREATE DATABASE IF NOT EXISTS labcare
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE labcare;

CREATE TABLE IF NOT EXISTS equipos (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(20) NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    ubicacion VARCHAR(100) NOT NULL,
    estado VARCHAR(20) NOT NULL,
    CONSTRAINT uq_equipos_codigo UNIQUE (codigo)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS solicitudes (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    codigo_solicitud VARCHAR(25) NOT NULL,
    equipo_id INT NOT NULL,
    solicitante VARCHAR(100) NOT NULL,
    descripcion VARCHAR(2000) NOT NULL,
    prioridad VARCHAR(5) COLLATE utf8mb4_0900_as_cs NOT NULL,
    estado VARCHAR(12) COLLATE utf8mb4_0900_as_cs NOT NULL DEFAULT 'PENDIENTE',
    fecha_registro DATETIME(6) NOT NULL,
    fecha_actualizacion DATETIME(6) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_solicitudes_codigo UNIQUE (codigo_solicitud),
    CONSTRAINT fk_solicitudes_equipo FOREIGN KEY (equipo_id) REFERENCES equipos (id),
    CONSTRAINT ck_solicitudes_prioridad CHECK (prioridad IN ('BAJA', 'MEDIA', 'ALTA')),
    CONSTRAINT ck_solicitudes_estado CHECK (estado IN ('PENDIENTE', 'EN_PROCESO', 'RESUELTA', 'CANCELADA')),
    CONSTRAINT ck_solicitudes_solicitante CHECK (CHAR_LENGTH(TRIM(solicitante)) >= 3),
    CONSTRAINT ck_solicitudes_descripcion CHECK (CHAR_LENGTH(TRIM(descripcion)) >= 10),
    CONSTRAINT ck_solicitudes_activo CHECK (activo IN (0, 1)),
    INDEX ix_solicitudes_activo_fecha (activo, fecha_registro)
) ENGINE=InnoDB;

INSERT INTO equipos (codigo, nombre, ubicacion, estado) VALUES
    ('EQ-001', 'Computadora de escritorio', 'Laboratorio 1', 'OPERATIVO'),
    ('EQ-002', 'Proyector multimedia', 'Aula 204', 'OPERATIVO'),
    ('EQ-003', 'Router de laboratorio', 'Laboratorio de Redes', 'OPERATIVO'),
    ('EQ-004', 'Impresora de oficina', 'Secretaría', 'OPERATIVO')
ON DUPLICATE KEY UPDATE codigo = equipos.codigo;
