/*
 * Static Routes
 */

namespace CessnaCrew.WebApi.Infrastructure
{
    /// <summary>
    /// Alle API-Routen gemäss Endpunkt-Tabelle (Dokumentation, Kapitel 4.2).
    /// Verwendung direkt an der Action, z. B. [HttpPost(StaticRoutes.Auth.Register)].
    /// </summary>
    public static class StaticRoutes
    {
        public const string Root = "api";

        // 1 Benutzerverwaltung
        public static class Auth
        {
            public const string Base = Root + "/auth";

            public const string Register = Base + "/register";           // POST
            public const string Login = Base + "/login";                 // POST
        }

        public static class Users
        {
            public const string Base = Root + "/users";

            public const string GetAll = Base;                           // GET
            public const string Me = Base + "/me";                       // GET
            public const string ById = Base + "/{id}";                   // GET, PUT, DELETE
        }

        // 2 Belegungskalender & Törnplanung
        public static class Turns
        {
            public const string Base = Root + "/turns";

            public const string GetAll = Base;                           // GET (?from&to), POST
            public const string History = Base + "/history";             // GET
            public const string ById = Base + "/{id}";                   // GET, PUT, DELETE
            public const string Complete = Base + "/{id}/complete";      // POST
        }

        // 3 Slot-Tausch
        public static class SwapRequests
        {
            public const string Base = Root + "/swap-requests";

            public const string ForTurn = Turns.Base + "/{id}/swap-requests";  // POST
            public const string GetAll = Base;                                 // GET
            public const string ById = Base + "/{id}";                         // PUT, DELETE
        }

        // 4 Crew & Einladungen
        public static class Crew
        {
            public const string Base = Turns.Base + "/{id}/crew";

            public const string GetAll = Base;                                    // GET, POST
            public const string ById = Base + "/{memberId}";                      // PUT, DELETE
            public const string InviteToken = Turns.Base + "/{id}/invite-token";  // POST, DELETE
        }

        public static class Invites
        {
            public const string Base = Root + "/invites";

            public const string ByToken = Base + "/{token}";             // GET
            public const string Join = Base + "/{token}/join";           // POST
        }

        // 5 Logbuch
        public static class Logbook
        {
            public const string Base = Turns.Base + "/{id}/logbook";

            public const string ForTurn = Base;                          // GET, PUT
            public const string Rows = Base + "/rows";                   // POST
            public const string RowById = Base + "/rows/{rowId}";        // PUT, DELETE
        }

        public static class Weather
        {
            public const string Base = Root + "/weather";

            public const string Get = Base;                              // GET (?lat&lon)
        }

        // 6 Kosten-Splitter & Jahresbilanz
        public static class Invoices
        {
            public const string Base = Root + "/invoices";

            public const string GetAll = Base;                           // GET (?year), POST
            public const string ById = Base + "/{id}";                   // GET, PUT, DELETE
            public const string SplitById = Base + "/{id}/splits/{splitId}";  // PUT
        }

        public static class Balance
        {
            public const string Base = Root + "/balance";

            public const string ByYear = Base + "/{year}";               // GET
        }
    }
}
