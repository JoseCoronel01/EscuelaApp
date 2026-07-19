        CREATE TABLE IF NOT EXISTS Alumnos (
            Id                INTEGER PRIMARY KEY AUTOINCREMENT,
            Matricula         TEXT NOT NULL UNIQUE,
            Nombre            TEXT NOT NULL,
            ApellidoPaterno   TEXT NOT NULL,
            ApellidoMaterno   TEXT,
            FechaNacimiento   TEXT NOT NULL,
            Genero            TEXT NOT NULL,
            Email             TEXT,
            Telefono          TEXT,
            Direccion         TEXT,
            Activo            INTEGER NOT NULL DEFAULT 1,
            FechaRegistro     TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE IF NOT EXISTS Maestros (
            Id                INTEGER PRIMARY KEY AUTOINCREMENT,
            NumeroEmpleado    TEXT NOT NULL UNIQUE,
            Nombre            TEXT NOT NULL,
            ApellidoPaterno   TEXT NOT NULL,
            ApellidoMaterno   TEXT,
            Especialidad      TEXT,
            Email             TEXT,
            Telefono          TEXT,
            Activo            INTEGER NOT NULL DEFAULT 1,
            FechaRegistro     TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE IF NOT EXISTS Materias (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            Clave            TEXT NOT NULL UNIQUE,
            Nombre           TEXT NOT NULL,
            Descripcion      TEXT,
            Creditos         INTEGER NOT NULL DEFAULT 0,
            HorasSemanales   INTEGER NOT NULL DEFAULT 0,
            Activo           INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS Grupos (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre           TEXT NOT NULL,
            Grado            TEXT NOT NULL,
            Turno            TEXT NOT NULL,
            CicloEscolar     INTEGER NOT NULL,
            MaestroId        INTEGER NOT NULL,
            CapacidadMaxima  INTEGER NOT NULL DEFAULT 30,
            Activo           INTEGER NOT NULL DEFAULT 1,
            FOREIGN KEY (MaestroId) REFERENCES Maestros(Id)
        );

        CREATE TABLE IF NOT EXISTS Inscripciones (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            AlumnoId         INTEGER NOT NULL,
            GrupoId          INTEGER NOT NULL,
            MateriaId        INTEGER NOT NULL,
            FechaInscripcion TEXT NOT NULL DEFAULT (datetime('now')),
            Estado           TEXT NOT NULL DEFAULT 'Activa',
            CicloEscolar     TEXT NOT NULL,
            FOREIGN KEY (AlumnoId)  REFERENCES Alumnos(Id),
            FOREIGN KEY (GrupoId)   REFERENCES Grupos(Id),
            FOREIGN KEY (MateriaId) REFERENCES Materias(Id)
        );

        CREATE TABLE IF NOT EXISTS Calificaciones (
            Id                   INTEGER PRIMARY KEY AUTOINCREMENT,
            InscripcionId        INTEGER NOT NULL,
            AlumnoId             INTEGER NOT NULL,
            MateriaId            INTEGER NOT NULL,
            GrupoId              INTEGER NOT NULL,
            Parcial1             REAL NOT NULL DEFAULT 0,
            Parcial2             REAL NOT NULL DEFAULT 0,
            Parcial3             REAL NOT NULL DEFAULT 0,
            Promedio             REAL NOT NULL DEFAULT 0,
            Estado               TEXT NOT NULL DEFAULT 'En Curso',
            Periodo              TEXT NOT NULL,
            Observaciones        TEXT,
            FechaActualizacion   TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id),
            FOREIGN KEY (AlumnoId)      REFERENCES Alumnos(Id),
            FOREIGN KEY (MateriaId)     REFERENCES Materias(Id),
            FOREIGN KEY (GrupoId)       REFERENCES Grupos(Id)
        );