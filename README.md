# WildHaven
WildHaven es una plataforma de gestión de reportes de vida silvestre con una API REST genérica CRUD multi-base de datos y un frontend Blazor Server. Incluye autenticación JWT, autorización por roles, notificaciones en tiempo real mediante el patrón Observer y soporte para SQL Server, PostgreSQL, MySQL y MariaDB. Program.

🏗️ Arquitectura
Backend: API web ASP.NET Core (webapicsharp) con controlador genérico EntidadesController que expone CRUD dinámico sobre cualquier tabla. 

Frontend: Aplicación Blazor Server (PresentacionWildHaven) con páginas públicas y autenticadas, incluyendo login y dashboard.
Patrón Observer: Notificaciones a dashboard, estadísticas y usuarios cuando cambia el estado de un reporte. 
Base de datos: Scripts SQL para crear esquema con tablas de usuarios, roles, reportes y especies. 

🛠️ Tecnologías
.NET 8 (ASP.NET Core, Blazor Server)
Entity Framework Core / ADO.NET según proveedor
Autenticación JWT (Bearer) 
Swagger/OpenAPI para documentación
MySQL/PostgreSQL/SQL Server/MariaDB (configurable) 
BCrypt para encriptación de campos específicos 

📦 Instalación y configuración
Clonar el repositorio.
Configurar la cadena de conexión y el proveedor de base de datos en appsettings.json (clave DatabaseProvider).
Ejecutar el script de base de datos correspondiente (ej. SQLQuery1.sql para SQL Server). 
Restaurar paquetes NuGet y compinar la solución.
Ejecutar webapicsharp (API) y PresentacionWildHaven (frontend).

🚀 Uso
API: Explore los endpoints genéricos en /swagger o use /api/info para ayuda.
Frontend: Inicie sesión en /login; los permisos se cargan según roles y rutas asignadas. 
Observers: Al cambiar un reporte, se actualizan dashboard, estadísticas y se notifica al usuario.
