using ContainerExpressions.Containers;
using Std.Out.Services;
using System;

namespace Std.Out.Models
{
    /// <summary>Creates a deterministic key from the merged parameters, in order to query the lastest correlation Id for said key in the future.</summary>
    public sealed class StorageKey
    {
        /// <summary>A name to represent the program / service storing the correlation Id (i.e. customer_service).</summary>
        public StdApplication Application { get; }

        /// <summary>The stage the request is running in (i.e. uat).</summary>
        public StdEnvironment Environment { get; }

        /// <summary>A unique identifier for the user running the current request (i.e. 12345670).</summary>
        public StdUser User { get; }

        /// <summary>The main outcome / objective of this request (i.e. save_customer).</summary>
        public Either<StdAction, StdNamespace> Action { get; }

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public StorageKey(NotNull<StdApplication> application, Either<NotNull<StdAction>, NotNull<StdNamespace>> action)
        {
            Application = application;
            Environment = null;
            User = null;
            Action = action.Match(x => new Either<StdAction, StdNamespace>(x), y => new Either<StdAction, StdNamespace>(y));
        }

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{environment}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="environment">The stage the request is running in, such as: local, dev, uat, etc.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public StorageKey(NotNull<StdApplication> application, NotNull<StdEnvironment> environment, Either<NotNull<StdAction>, NotNull<StdNamespace>> action)
        {
            Application = application;
            Environment = environment;
            User = null;
            Action = action.Match(x => new Either<StdAction, StdNamespace>(x), y => new Either<StdAction, StdNamespace>(y));
        }

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{user}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="user">A unique identifier for the user running the current request.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public StorageKey(NotNull<StdApplication> application, NotNull<StdUser> user, Either<NotNull<StdAction>, NotNull<StdNamespace>> action)
        {
            Application = application;
            Environment = null;
            User = user;
            Action = action.Match(x => new Either<StdAction, StdNamespace>(x), y => new Either<StdAction, StdNamespace>(y));
        }

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{environment}/{user}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="environment">The stage the request is running in, such as: local, dev, uat, etc.</param>
        /// <param name="user">A unique identifier for the user running the current request.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public StorageKey(NotNull<StdApplication> application, NotNull<StdEnvironment> environment, NotNull<StdUser> user, Either<NotNull<StdAction>, NotNull<StdNamespace>> action)
        {
            Application = application;
            Environment = environment;
            User = user;
            Action = action.Match(x => new Either<StdAction, StdNamespace>(x), y => new Either<StdAction, StdNamespace>(y));
        }

        /// <summary>Returns the key in the format: {application}/{environment}/{user}/{action}/correlation.json (assuming each parameter is set).</summary>
        public override string ToString()
        {
            string key = Application;
            if (Environment is not null) key = $"{key}/{Environment}";
            if (User is not null) key = $"{key}/{User}";
            string action = Action.Match(x => x.Value, y => Util.GetCaller(y.Value.Namespace, y.Value.Offset));
            return $"{key}/{action}/correlation.json";
        }

        /// <summary>Returns just the action's subsection, or the full key path.</summary>
        /// <param name="getActionPath">When true returns: {action}/correlation.json, otherwise returns the whole key path.</param>
        public string ToString(bool getActionPath)
        {
            if (!getActionPath) return ToString();
            var action = Action.Match(x => x.Value, y => Util.GetCaller(y.Value.Namespace, y.Value.Offset));
            return $"{action}/correlation.json";
        }

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public static StorageKey Create(string application, Either<string, (string Namespace, int Offset)> action) => new StorageKey(new StdApplication(application), action.Match(x => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdAction(x)), y => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdNamespace(y))));

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{environment}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="environment">The stage the request is running in, such as: local, dev, uat, etc.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public static StorageKey CreateWithEnvironment(string application, string environment, Either<string, (string Namespace, int Offset)> action) => new StorageKey(new StdApplication(application), new StdEnvironment(environment), action.Match(x => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdAction(x)), y => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdNamespace(y))));

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{user}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="user">A unique identifier for the user running the current request.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public static StorageKey CreateWithUser(string application, string user, Either<string, (string Namespace, int Offset)> action) => new StorageKey(new StdApplication(application), new StdUser(user), action.Match(x => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdAction(x)), y => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdNamespace(y))));

        /// <summary>
        /// Allowed parameter values are: 'a-z', 'A-Z', '0-9', and special characters: '._-' (dot, underscore, and hyphen).
        /// <para>Key: {application}/{environment}/{user}/{action}/correlation.json</para>
        /// </summary>
        /// <param name="application">A name to represent the program / service storing the correlation Id.</param>
        /// <param name="environment">The stage the request is running in, such as: local, dev, uat, etc.</param>
        /// <param name="user">A unique identifier for the user running the current request.</param>
        /// <param name="action">The main outcome / objective of this request, such as: save_customer, process_transaction, etc.</param>
        public static StorageKey CreateWithEnvironmentAndUser(string application, string environment, string user, Either<string, (string Namespace, int Offset)> action) => new StorageKey(new StdApplication(application), new StdEnvironment(environment), new StdUser(user), action.Match(x => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdAction(x)), y => new Either<NotNull<StdAction>, NotNull<StdNamespace>>(new StdNamespace(y))));
    }

    /// <summary>A name to represent the program / service storing the correlation Id (i.e. customer_service).</summary>
    public sealed class StdApplication : Alias<string> { public StdApplication(string value) : base(value.ThrowIf(KeyValidator.IsNotValid)) { } }

    /// <summary>The stage the request is running in (i.e. uat).</summary>
    public sealed class StdEnvironment : Alias<string> { public StdEnvironment(string value) : base(value.ThrowIf(KeyValidator.IsNotValid)) { } }

    /// <summary>A unique identifier for the user running the current request (i.e. 12345670).</summary>
    public sealed class StdUser : Alias<string> { public StdUser(string value) : base(value.ThrowIf(KeyValidator.IsNotValid)) { } }

    /// <summary>The main outcome / objective of this request (i.e. save_customer).</summary>
    public sealed class StdAction : Alias<string> { public StdAction(string value) : base(value.ThrowIf(KeyValidator.IsNotValid)) { } }

    /// <summary>Pulls the StdAction from the top level calling method defined in the namespace. Offset may be used to go down a function call (i.e. to skip middleware etc).</summary>
    public sealed class StdNamespace : Alias<(string Namespace, int Offset)> { public StdNamespace((string Namespace, int Offset) value) : base(value.ThrowIf(KeyValidator.IsNotValid)) { } }

    file static class KeyValidator
    {
        public static bool IsNotValid((string, int) value)
        {
            var isNamespaceValid = IsValid(value.Item1);
            var isOffsetValid = value.Item2 >= 0;
            var isValid = isNamespaceValid && isOffsetValid;
            return !isValid;
        }

        public static bool IsNotValid(string value) => !IsValid(value);

        /// <summary>Enforce the allowed characters: 'a-z', 'A-Z', '0-9', and '._-'.</summary>
        public static bool IsValid(string value)
        {
            var isValid = !string.IsNullOrWhiteSpace(value);

            if (isValid)
            {
                foreach (var c in value)
                {
                    if (!IsLetter(c) && !IsDigit(c) && !IsSpecialCharacter(c))
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            return isValid;
        }

        private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        private static bool IsDigit(char c) => c >= '0' && c <= '9';
        private static bool IsSpecialCharacter(char c) => c == '.' || c == '_' || c == '-';
    }
}
