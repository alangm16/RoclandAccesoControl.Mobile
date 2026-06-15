using System.Text.Json.Serialization;

namespace RoclandAccesoControl.Mobile.Models;

public class SolicitudPendiente
{
    [JsonPropertyName("solicitudId")]
    public int SolicitudId { get; set; }
    [JsonPropertyName("registroId")]
    public int RegistroId { get; set; }
    [JsonPropertyName("tipoRegistro")]
    public string TipoRegistro { get; set; } = string.Empty;
    [JsonPropertyName("personaId")]
    public int PersonaId { get; set; }
    [JsonPropertyName("nombrePersona")]
    public string NombrePersona { get; set; } = string.Empty;
    [JsonPropertyName("empresa")]
    public string? Empresa { get; set; }
    [JsonPropertyName("numeroIdentificacion")]
    public string? NumeroIdentificacion { get; set; }
    [JsonPropertyName("tipoID")]
    public string TipoID { get; set; } = string.Empty;
    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty;
    [JsonPropertyName("area")]
    public string? Area { get; set; }
    [JsonPropertyName("placas")]
    public string? Placas { get; set; }
    [JsonPropertyName("fechaSolicitud")]
    public DateTime FechaSolicitud { get; set; }
    [JsonPropertyName("tieneFoto")]
    public bool TieneFoto { get; set; }

    // Propiedades auxiliares (sin mapeo JSON)
    public string TipoIcono => TipoRegistro == "Visitante" ? "icon_visitor.png" : "icon_truck.png";
    public string TipoColor => TipoRegistro == "Visitante" ? "#2563EB" : "#7C3AED";
    public string HoraFormateada => FechaSolicitud.ToLocalTime().ToString("HH:mm");
    public string AreaOEmpresa => TipoRegistro == "Visitante" ? (Area ?? "") : (Empresa ?? "");
}

public class AccesoActivo
{
    [JsonPropertyName("registroId")]
    public int RegistroId { get; set; }
    [JsonPropertyName("tipoRegistro")]
    public string TipoRegistro { get; set; } = string.Empty;
    [JsonPropertyName("nombrePersona")]
    public string NombrePersona { get; set; } = string.Empty;
    [JsonPropertyName("empresa")]
    public string? Empresa { get; set; }
    [JsonPropertyName("numeroGafete")]
    public string NumeroGafete { get; set; } = string.Empty;
    [JsonPropertyName("fechaEntrada")]
    public DateTime FechaEntrada { get; set; }
    [JsonPropertyName("area")]
    public string Area { get; set; } = string.Empty;
    [JsonPropertyName("minutosLlevaDentro")]
    public double MinutosLlevaDentro { get; set; }

    private readonly DateTime _horaCreacionLocal = DateTime.UtcNow;

    public string TipoIcono => TipoRegistro == "Visitante" ? "icon_visitor.png" : "icon_truck.png";
    public string HoraEntradaFormateada => FechaEntrada.ToLocalTime().ToString("HH:mm");

    public string TiempoTranscurrido
    {
        get
        {
            var tiempoEnPantalla = DateTime.UtcNow - _horaCreacionLocal;
            var minutosTotales = MinutosLlevaDentro + tiempoEnPantalla.TotalMinutes;
            if (minutosTotales < 0) minutosTotales = 0;
            int horas = (int)(minutosTotales / 60);
            int minutos = (int)(minutosTotales % 60);
            return horas >= 1 ? $"{horas}h {minutos}m" : $"{minutos}m";
        }
    }
}

public class AprobarRequest
{
    [JsonPropertyName("solicitudId")]
    public int SolicitudId { get; set; }
    [JsonPropertyName("gafeteId")]
    public int GafeteId { get; set; }
}

public class RechazarRequest
{
    [JsonPropertyName("solicitudId")]
    public int SolicitudId { get; set; }
    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }
}

public class MarcarSalidaRequest
{
    [JsonPropertyName("registroId")]
    public int RegistroId { get; set; }
    [JsonPropertyName("tipoRegistro")]
    public string TipoRegistro { get; set; } = string.Empty;
}

public class NuevaSolicitudEvent
{
    [JsonPropertyName("solicitudId")]
    public int SolicitudId { get; set; }
    [JsonPropertyName("registroId")]
    public int RegistroId { get; set; }
    [JsonPropertyName("personaId")]
    public int PersonaId { get; set; }
    [JsonPropertyName("tipoRegistro")]
    public string TipoRegistro { get; set; } = string.Empty;
    [JsonPropertyName("nombrePersona")]
    public string NombrePersona { get; set; } = string.Empty;
    [JsonPropertyName("empresa")]
    public string? Empresa { get; set; }
    [JsonPropertyName("numeroIdentificacion")]
    public string? NumeroIdentificacion { get; set; }
    [JsonPropertyName("tipoID")]
    public string TipoID { get; set; } = string.Empty;
    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty;
    [JsonPropertyName("area")]
    public string? Area { get; set; }
    [JsonPropertyName("fechaSolicitud")]
    public DateTime FechaSolicitud { get; set; }

    [JsonPropertyName("tieneFoto")]
    public bool TieneFoto { get; set; }
}

public class GafeteDisponible
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;
}

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class QrLoginRequest
{
    [JsonPropertyName("qrCode")]
    public string QRCode { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string Token { get; set; } = string.Empty;
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("expiracion")]
    public DateTime Expiracion { get; set; }
    [JsonPropertyName("usuario")]
    public UsuarioTokenDto? Usuario { get; set; }

    // Propiedades de conveniencia (mapeadas desde Usuario)
    public string NombreCompleto => Usuario?.NombreCompleto ?? string.Empty;
    public string Username => Usuario?.Username ?? string.Empty;
    public int UsuarioId => Usuario?.Id ?? 0;
}

public class UsuarioTokenDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class MiPerfilResponse
{
    [JsonPropertyName("perfilId")]
    public int PerfilId { get; set; }
    [JsonPropertyName("superAdminUsuarioId")]
    public int SuperAdminUsuarioId { get; set; }
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;
    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } = string.Empty;   // antes TipoPerfil
    [JsonPropertyName("nivelRol")]
    public int NivelRol { get; set; }
    [JsonPropertyName("turno")]
    public string? Turno { get; set; }
    [JsonPropertyName("numeroEmpleado")]
    public string? NumeroEmpleado { get; set; }
}

public class AppResumedMessage { }