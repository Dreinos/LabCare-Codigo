# Configurar MySQL para LabCare

## Crear la base de datos

Necesitas MySQL 8.0.16 o posterior y el SDK de .NET 8.

Abre y ejecuta [database/labcare.sql](database/labcare.sql) en MySQL Workbench con un usuario autorizado a crear la base de datos. El script crea el esquema **labcare**, las tablas y los cuatro equipos de ejemplo. Puedes ejecutarlo de nuevo conservando los datos existentes.

## Conectar la aplicación

La cuenta que use LabCare necesita permiso de lectura sobre **labcare.equipos** y permisos de lectura, inserción y actualización sobre **labcare.solicitudes**.

Desde la carpeta que contiene **LabCare.csproj**, reemplaza **TU_USUARIO** y **TU_CONTRASEÑA** con los datos de tu instancia. Ajusta el servidor o puerto si tu configuración es distinta.

~~~powershell
dotnet user-secrets set 'ConnectionStrings:LabCare' 'Server=127.0.0.1;Port=3306;Database=labcare;User ID=TU_USUARIO;Password=TU_CONTRASEÑA'
dotnet run --launch-profile LabCare
~~~

El perfil **LabCare** carga la conexión desde los secretos locales de .NET. Si ejecutas la aplicación con otro perfil, también puedes configurar la variable de entorno **ConnectionStrings__LabCare**.

Abre [http://localhost:5086/solicitudes](http://localhost:5086/solicitudes) y verifica que el formulario muestra los cuatro equipos.
