using System.Text.Json.Serialization;

namespace RoclandAccesoControl.Mobile.Models;

public class SolicitudPendiente
{
    public int SolicitudId { get; set; }
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public int PersonaId { get; set; }
    public string NombrePersona { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string TipoID { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Placas { get; set; }     // UnidadPlacas del RegistroProveedor; null para Visitante
    public DateTime FechaSolicitud { get; set; }

    // Computed para la UI
    public string TipoIcono => TipoRegistro == "Visitante" ? "icon_visitor.png" : "icon_truck.png";
    public string TipoColor => TipoRegistro == "Visitante" ? "#2563EB" : "#7C3AED";
    public string HoraFormateada => FechaSolicitud.ToLocalTime().ToString("HH:mm");
    public string AreaOEmpresa => TipoRegistro == "Visitante" ? (Area ?? "") : (Empresa ?? "");
    public string TipoIconoSource => TipoRegistro switch
    {
        "Visitante" => "icon_visitor.png",
        "Proveedor/Cliente" => "icon_truck.png",
        _ => "icon_visitor.png"
    };
}

public class AccesoActivo
{
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public string NombrePersona { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string NumeroGafete { get; set; } = string.Empty;
    public DateTime FechaEntrada { get; set; }
    public string Area { get; set; } = string.Empty;

    public double MinutosLlevaDentro { get; set; } 

    // 2. Guardamos la hora en que el celular creó/descargó este objeto en memoria
    private readonly DateTime _horaCreacionLocal = DateTime.UtcNow;

    public string TipoIcono => TipoRegistro == "Visitante" ? "icon_visitor.png" : "icon_truck.png";
    public string HoraEntradaFormateada => FechaEntrada.ToLocalTime().ToString("HH:mm");
    
    // 3. El cálculo blindado
    public string TiempoTranscurrido
    {
        get
        {
            var tiempoEnPantalla = DateTime.UtcNow - _horaCreacionLocal;
            var minutosTotales = MinutosLlevaDentro + tiempoEnPantalla.TotalMinutes;

            if (minutosTotales < 0) minutosTotales = 0;

            int horas = (int)(minutosTotales / 60);
            int minutos = (int)(minutosTotales % 60);

            if (horas >= 1)
                return $"{horas}h {minutos}m";
            return $"{minutos}m";
        }
    }
}

// Modificar AprobarRequest para usar GafeteId en lugar de string
public class AprobarRequest
{
    public int SolicitudId { get; set; }
    public int GuardiaId { get; set; }
    public int GafeteId { get; set; }               // <-- Ahora ID del gafete
    // Podemos mantener NumeroGafete para mostrarlo en UI, pero no es necesario enviarlo
}
public class RechazarRequest
{
    public int SolicitudId { get; set; }
    public int GuardiaId { get; set; }
    public string? Motivo { get; set; }
}

public class MarcarSalidaRequest
{
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public int GuardiaId { get; set; }
}

// Evento que llega por SignalR
public class NuevaSolicitudEvent
{
    public int SolicitudId { get; set; }
    public int RegistroId { get; set; }
    public string TipoRegistro { get; set; } = string.Empty;
    public string NombrePersona { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string TipoID { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Area { get; set; }
    public DateTime FechaSolicitud { get; set; }
}

// Modelo para gafete disponible (retornado por API)
public class GafeteDisponible
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    // Puedes agregar Observaciones si la tabla lo tuviera
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class QrLoginRequest
{
    public string QRCode { get; set; }
}

public class LoginResponse
{
    // Le decimos que el "accessToken" del JSON se guarde en nuestra propiedad "Token"
    [JsonPropertyName("accessToken")]
    public string Token { get; set; }

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("accessTokenExpira")]
    public DateTime Expiracion { get; set; }

    // Opcional, pero muy útil ya que el backend te lo está mandando:
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }
}

// --- DTOs para Acceso Control ---
public class MiPerfilResponse
{
    public int Id { get; set; } // PerfilId de AccesoControl
    public int SuperAdminUsuarioId { get; set; }
    public string NombreCompleto { get; set; }
    public string TipoPerfil { get; set; } // Guardia, Administrador, etc.
    public string Turno { get; set; }
    public string NumeroEmpleado { get; set; }
}