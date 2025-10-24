CREATE DATABASE BDWILDHAVEN
GO
USE BDWILDHAVEN 
GO

--MODULO DE USUARIOS Y AUNTENTICACION
CREATE TABLE Roles (
    RolID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL UNIQUE, 
    Descripcion VARCHAR(255),
    FechaCreacion DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE Usuarios (
    UsuarioID INT IDENTITY(1,1) PRIMARY KEY,
    RolID INT NOT NULL FOREIGN KEY REFERENCES Roles(RolID),
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Telefono VARCHAR(20),
    FechaRegistro DATETIME2 DEFAULT GETDATE(),
    Activo BIT DEFAULT 1,
    TokenRecuperacion VARCHAR(100),
    TokenExpiracion DATETIME2
);

-- MODULO DE REPORTES Y FAUNA
CREATE TABLE Especies (
    EspecieID INT IDENTITY(1,1) PRIMARY KEY,
    NombreComun VARCHAR(100) NOT NULL,
    NombreCientifico VARCHAR(150),
    Descripcion TEXT,
    Activo BIT DEFAULT 1
);

CREATE TABLE EstadosReporte (
    EstadoID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL UNIQUE, 
    Descripcion VARCHAR(255),
    Orden INT NOT NULL
);

CREATE TABLE Reportes (
    ReporteID INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    EspecieID INT FOREIGN KEY REFERENCES Especies(EspecieID),
    DescripcionEspecie VARCHAR(150), 
    EstadoAnimal VARCHAR(100),
    DireccionTexto VARCHAR(500),
    EstadoID INT FOREIGN KEY REFERENCES EstadosReporte(EstadoID),
    FechaCreacion DATETIME2 DEFAULT GETDATE(),
    FechaActualizacion DATETIME2 DEFAULT GETDATE(),
);
CREATE TABLE ReporteSinRegistro (
    id INT IDENTITY(1,1) PRIMARY KEY, 
    [name] NVARCHAR(100) NULL,
    number NVARCHAR(50) NULL,
    typePet NVARCHAR(50) NULL,
    direcction NVARCHAR(150) NULL,
    addictionalInformation NVARCHAR(500) NULL
);


--MODULO MULTIMEDIA
CREATE TABLE Multimedia (
    MultimediaID INT IDENTITY(1,1) PRIMARY KEY,
    ReporteID INT NOT NULL FOREIGN KEY REFERENCES Reportes(ReporteID),
    TipoArchivo VARCHAR(50) NOT NULL, 
    NombreArchivo VARCHAR(255) NOT NULL,
    RutaAlmacenamiento VARCHAR(500) NOT NULL,
    FechaSubida DATETIME2 DEFAULT GETDATE(),
);
--MODULO CLINICO Y ANIMALES
CREATE TABLE Animales (
    AnimalID INT IDENTITY(1,1) PRIMARY KEY,
    EspecieID INT NOT NULL FOREIGN KEY REFERENCES Especies(EspecieID),
    ReporteID INT NOT NULL FOREIGN KEY REFERENCES Reportes(ReporteID),
    Nombre VARCHAR(100),
    CodigoIdentificacion VARCHAR(50) UNIQUE,
    FechaIngreso DATETIME2 NOT NULL,
    EstadoSalud VARCHAR(100), 
    FechaLiberacion DATETIME2,
    LugarLiberacion VARCHAR(255),
    Observaciones TEXT
);

CREATE TABLE SeguimientosClinicos (
    SeguimientoID INT IDENTITY(1,1) PRIMARY KEY,
    AnimalID INT NOT NULL FOREIGN KEY REFERENCES Animales(AnimalID),
    UsuarioID INT NOT NULL FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    Fecha DATETIME2 DEFAULT GETDATE(),
    TipoSeguimiento VARCHAR(100),
    Diagnostico TEXT,
    Tratamiento TEXT,
    Medicamentos TEXT,
    CostoTratamiento DECIMAL(10,2),
    ProximaRevision DATETIME2
);
--MODULO DE APADRINAMIENTO
CREATE TABLE Apadrinamientos (
    ApadrinamientoID INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID INT NOT NULL FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    AnimalID INT NOT NULL FOREIGN KEY REFERENCES Animales(AnimalID),
    FechaInicio DATETIME2 DEFAULT GETDATE(),
    FechaFin DATETIME2,
    MontoMensual DECIMAL(10,2),
    Comentarios TEXT
);

CREATE TABLE Donaciones (
    DonacionID INT IDENTITY(1,1) PRIMARY KEY,
    ApadrinamientoID INT FOREIGN KEY REFERENCES Apadrinamientos(ApadrinamientoID),
    UsuarioID INT NOT NULL FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    FechaDonacion DATETIME2 DEFAULT GETDATE(),
);
--MODULO DE NOTIFICACIONES Y TRAZABILIDAD
CREATE TABLE HistorialAcciones (
    HistorialID INT IDENTITY(1,1) PRIMARY KEY,
    ReporteID INT NOT NULL FOREIGN KEY REFERENCES Reportes(ReporteID),
    UsuarioID INT NOT NULL FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    Accion VARCHAR(100) NOT NULL, 
    Descripcion TEXT,
    FechaAccion DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE Notificaciones (
    NotificacionID INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID INT NOT NULL FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    Tipo VARCHAR(50) NOT NULL, 
    Titulo VARCHAR(255) NOT NULL,
    Mensaje TEXT NOT NULL,
    FechaEnvio DATETIME2 DEFAULT GETDATE(),
    EntidadRelacionada VARCHAR(50), 
    EntidadID INT 
);

CREATE TABLE Auditoria (
    AuditoriaID INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    Accion VARCHAR(100) NOT NULL,
    TablaAfectada VARCHAR(100) NOT NULL,
    RegistroID INT NOT NULL,
    ValoresAnteriores TEXT,
    ValoresNuevos TEXT,
    FechaAccion DATETIME2 DEFAULT GETDATE(),
);