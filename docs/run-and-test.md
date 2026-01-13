# Ejecutar y testear (Backend + Frontend)

## 1) Comandos para ejecutar

Backend (puerto 5002):
```
dotnet run --project src/Schedia.Api --environment Development --urls "http://localhost:5002"
```

Frontend (puerto 5174):
```
dotnet-serve -d d:\Work\Schedia\frontend -p 5174
```

## 2) URLs de prueba

Frontend:
- `http://localhost:5174?hostId=1`
- `http://localhost:5174?hostId=1&lang=es&duration=30`
- `http://localhost:5174?hostId=1&apiBase=http://localhost:5002`

Backend:
- Meta: `http://localhost:5002/api/meta?hostId=1&lang=es`
- Availability (POST): `http://localhost:5002/api/availability`
- Book (POST): `http://localhost:5002/api/book`
- Swagger: `http://localhost:5002/swagger`
