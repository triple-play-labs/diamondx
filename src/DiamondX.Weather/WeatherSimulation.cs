using SimulationEngine.Core;
using SimulationEngine.Orchestration;
using SimulationEngine.Random;

namespace DiamondX.Weather;

/// <summary>
/// Simulates weather conditions that evolve over the course of a baseball game.
/// Publishes weather data to SharedContext for other models to consume.
/// </summary>
public sealed class WeatherSimulation : ISimulation
{
    private readonly WeatherConfig _config;
    private ISimulationContext? _context;
    private ISharedContext? _sharedContext;
    private WeatherConditions _current;
    private bool _gameDelayed;

    public string Name => "Weather";
    public string Version => "1.0.0";
    public bool IsComplete => false; // Weather runs continuously

    /// <summary>
    /// Current weather conditions.
    /// </summary>
    public WeatherConditions Current => _current;

    /// <summary>
    /// Whether the game is currently delayed due to weather.
    /// </summary>
    public bool IsGameDelayed => _gameDelayed;

    public WeatherSimulation() : this(new WeatherConfig())
    {
    }

    public WeatherSimulation(WeatherConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _current = config.InitialConditions;
    }

    public void Initialize(ISimulationContext context)
    {
        _context = context;

        // Check if we're in an orchestrated context with shared state
        if (context is OrchestratedModelContext orchestrated)
        {
            _sharedContext = orchestrated.Shared;
        }

        // Publish initial conditions
        PublishConditions();
    }

    public SimulationStepResult Step()
    {
        if (_context is null)
            throw new InvalidOperationException("Simulation not initialized.");

        // Evolve weather based on configuration
        EvolveWeather();

        // Publish updated conditions
        PublishConditions();

        // Check for rain delay
        if (!_current.IsPlayable && !_gameDelayed)
        {
            _gameDelayed = true;
            _sharedContext?.Set("weather.delayed", true);
            _sharedContext?.Set("weather.delay_reason", GetDelayReason());
        }
        else if (_current.IsPlayable && _gameDelayed)
        {
            _gameDelayed = false;
            _sharedContext?.Set("weather.delayed", false);
            _sharedContext?.Set("weather.delay_reason", "");
        }

        return SimulationStepResult.Continue;
    }

    private void EvolveWeather()
    {
        var random = _context!.Random;

        // Temperature drift (±0.5°F per step)
        var tempDelta = (random.NextDouble() - 0.5) * _config.TemperatureVolatility;
        var newTemp = Math.Clamp(_current.Temperature + tempDelta, _config.MinTemperature, _config.MaxTemperature);

        // Humidity drift
        var humidityDelta = (random.NextDouble() - 0.5) * _config.HumidityVolatility;
        var newHumidity = Math.Clamp(_current.Humidity + humidityDelta, 20, 100);

        // Wind speed changes
        var windDelta = (random.NextDouble() - 0.5) * _config.WindVolatility;
        var newWindSpeed = Math.Clamp(_current.WindSpeed + windDelta, 0, 50);

        // Wind direction shifts slowly
        var dirDelta = (random.NextDouble() - 0.5) * 10;
        var newWindDir = (_current.WindDirection + dirDelta + 360) % 360;

        // Pressure changes slowly
        var pressureDelta = (random.NextDouble() - 0.5) * 0.02;
        var newPressure = Math.Clamp(_current.Pressure + pressureDelta, 29.0, 31.0);

        // Precipitation changes
        var newPrecip = EvolvePrecipitation(random);

        // Sky condition tied to precipitation
        var newSky = newPrecip switch
        {
            PrecipitationType.Thunderstorm => SkyCondition.Overcast,
            PrecipitationType.Heavy => SkyCondition.Overcast,
            PrecipitationType.Moderate => SkyCondition.Overcast,
            PrecipitationType.Light => SkyCondition.Cloudy,
            PrecipitationType.Drizzle => SkyCondition.Cloudy,
            _ => EvolveSkyClearWeather(random)
        };

        _current = new WeatherConditions
        {
            Temperature = newTemp,
            Humidity = newHumidity,
            WindSpeed = newWindSpeed,
            WindDirection = newWindDir,
            Pressure = newPressure,
            Precipitation = newPrecip,
            Sky = newSky
        };
    }

    private PrecipitationType EvolvePrecipitation(IRandomSource random)
    {
        var roll = random.NextDouble();

        // Use lookup tables for cleaner transition logic
        return _current.Precipitation switch
        {
            PrecipitationType.None => roll < _config.RainChance ? PrecipitationType.Drizzle : PrecipitationType.None,
            PrecipitationType.Drizzle => GetDrizzleTransition(roll),
            PrecipitationType.Light => GetLightRainTransition(roll),
            PrecipitationType.Moderate => GetModerateRainTransition(roll),
            PrecipitationType.Heavy => GetHeavyRainTransition(roll),
            PrecipitationType.Thunderstorm => GetThunderstormTransition(roll),
            _ => PrecipitationType.None
        };
    }

    private static PrecipitationType GetDrizzleTransition(double roll)
    {
        if (roll < 0.7) return PrecipitationType.Drizzle;
        if (roll < 0.85) return PrecipitationType.Light;
        if (roll < 0.95) return PrecipitationType.None;
        return PrecipitationType.Moderate;
    }

    private static PrecipitationType GetLightRainTransition(double roll)
    {
        if (roll < 0.6) return PrecipitationType.Light;
        if (roll < 0.75) return PrecipitationType.Drizzle;
        if (roll < 0.85) return PrecipitationType.Moderate;
        if (roll < 0.95) return PrecipitationType.None;
        return PrecipitationType.Heavy;
    }

    private static PrecipitationType GetModerateRainTransition(double roll)
    {
        if (roll < 0.5) return PrecipitationType.Moderate;
        if (roll < 0.7) return PrecipitationType.Light;
        if (roll < 0.85) return PrecipitationType.Heavy;
        if (roll < 0.95) return PrecipitationType.Thunderstorm;
        return PrecipitationType.Drizzle;
    }

    private static PrecipitationType GetHeavyRainTransition(double roll)
    {
        if (roll < 0.4) return PrecipitationType.Heavy;
        if (roll < 0.6) return PrecipitationType.Moderate;
        if (roll < 0.8) return PrecipitationType.Thunderstorm;
        return PrecipitationType.Light;
    }

    private static PrecipitationType GetThunderstormTransition(double roll)
    {
        if (roll < 0.5) return PrecipitationType.Thunderstorm;
        if (roll < 0.8) return PrecipitationType.Heavy;
        return PrecipitationType.Moderate;
    }

    private SkyCondition EvolveSkyClearWeather(IRandomSource random)
    {
        var roll = random.NextDouble();

        return _current.Sky switch
        {
            SkyCondition.Clear => roll < 0.85 ? SkyCondition.Clear : SkyCondition.PartlyCloudy,
            SkyCondition.PartlyCloudy => GetPartlyCloudyTransition(roll),
            SkyCondition.Cloudy => GetCloudyTransition(roll),
            SkyCondition.Overcast => roll < 0.3 ? SkyCondition.Cloudy : SkyCondition.Overcast,
            _ => SkyCondition.Clear
        };
    }

    private static SkyCondition GetPartlyCloudyTransition(double roll)
    {
        if (roll < 0.3) return SkyCondition.Clear;
        if (roll < 0.7) return SkyCondition.PartlyCloudy;
        return SkyCondition.Cloudy;
    }

    private static SkyCondition GetCloudyTransition(double roll)
    {
        if (roll < 0.2) return SkyCondition.PartlyCloudy;
        if (roll < 0.7) return SkyCondition.Cloudy;
        return SkyCondition.Overcast;
    }

    private void PublishConditions()
    {
        if (_sharedContext is null)
            return;

        // Publish individual values for easy access
        _sharedContext.Set("weather.temperature", _current.Temperature);
        _sharedContext.Set("weather.humidity", _current.Humidity);
        _sharedContext.Set("weather.wind_speed", _current.WindSpeed);
        _sharedContext.Set("weather.wind_direction", _current.WindDirection);
        _sharedContext.Set("weather.pressure", _current.Pressure);
        _sharedContext.Set("weather.precipitation", _current.Precipitation.ToString());
        _sharedContext.Set("weather.sky", _current.Sky.ToString());
        _sharedContext.Set("weather.playable", _current.IsPlayable);
        _sharedContext.Set("weather.hr_modifier", _current.GetHomeRunModifier());
        _sharedContext.Set("weather.description", _current.GetDescription());

        // Also publish the full conditions object
        _sharedContext.Set("weather.conditions", _current);
    }

    private string GetDelayReason()
    {
        if (_current.Precipitation == PrecipitationType.Thunderstorm)
            return "Thunderstorm - dangerous conditions";
        if (_current.Precipitation == PrecipitationType.Heavy)
            return "Heavy rain - field unplayable";
        if (_current.WindSpeed >= 40)
            return "Dangerous wind conditions";
        return "Weather delay";
    }

    public void Dispose()
    {
        _context = null;
        _sharedContext = null;
    }
}

/// <summary>
/// Configuration for weather simulation behavior.
/// </summary>
public class WeatherConfig
{
    /// <summary>
    /// Initial weather conditions at game start.
    /// </summary>
    public WeatherConditions InitialConditions { get; set; } = new();

    /// <summary>
    /// Temperature change rate per step (±degrees).
    /// </summary>
    public double TemperatureVolatility { get; set; } = 1.0;

    /// <summary>
    /// Humidity change rate per step (±percent).
    /// </summary>
    public double HumidityVolatility { get; set; } = 2.0;

    /// <summary>
    /// Wind speed change rate per step (±mph).
    /// </summary>
    public double WindVolatility { get; set; } = 2.0;

    /// <summary>
    /// Chance of rain starting when currently dry (per step).
    /// </summary>
    public double RainChance { get; set; } = 0.02;

    /// <summary>
    /// Minimum temperature bound.
    /// </summary>
    public double MinTemperature { get; set; } = 40;

    /// <summary>
    /// Maximum temperature bound.
    /// </summary>
    public double MaxTemperature { get; set; } = 105;

    /// <summary>
    /// Create config for a specific weather scenario.
    /// </summary>
    public static WeatherConfig HotSummerDay() => new()
    {
        InitialConditions = new WeatherConditions
        {
            Temperature = 95,
            Humidity = 65,
            WindSpeed = 8,
            WindDirection = 180, // Blowing out
            Pressure = 30.1,
            Precipitation = PrecipitationType.None,
            Sky = SkyCondition.Clear
        },
        RainChance = 0.05 // Higher chance of afternoon storms
    };

    public static WeatherConfig WindyDay() => new()
    {
        InitialConditions = new WeatherConditions
        {
            Temperature = 68,
            Humidity = 40,
            WindSpeed = 25,
            WindDirection = 45, // From right field
            Pressure = 29.8,
            Precipitation = PrecipitationType.None,
            Sky = SkyCondition.PartlyCloudy
        },
        WindVolatility = 5.0
    };

    public static WeatherConfig RainyDay() => new()
    {
        InitialConditions = new WeatherConditions
        {
            Temperature = 58,
            Humidity = 85,
            WindSpeed = 12,
            WindDirection = 270, // From left field
            Pressure = 29.5,
            Precipitation = PrecipitationType.Light,
            Sky = SkyCondition.Overcast
        },
        RainChance = 0.3
    };

    public static WeatherConfig PerfectBaseball() => new()
    {
        InitialConditions = new WeatherConditions
        {
            Temperature = 72,
            Humidity = 45,
            WindSpeed = 5,
            WindDirection = 180, // Light breeze out
            Pressure = 29.95,
            Precipitation = PrecipitationType.None,
            Sky = SkyCondition.Clear
        },
        TemperatureVolatility = 0.5,
        WindVolatility = 1.0,
        RainChance = 0.01
    };
}
