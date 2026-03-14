namespace GraficasMixing.Helpers
{
    public static class LaborDayHelper
    {
        public static DateTime GetLaborDay(DateTime now)
        {
            // Convertir a hora local si viene en UTC
            now = now.ToLocalTime();

            TimeSpan hora = now.TimeOfDay;

            // Turno 3 → pertenece al día anterior
            if (hora >= new TimeSpan(23, 46, 0) || hora <= new TimeSpan(6, 59, 59))
            {
                return now.Date.AddDays(-1);
            }

            // Turno 1 y 2 → pertenece al día actual
            return now.Date;
        }
    }
}
