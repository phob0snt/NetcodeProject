using Unity.Services.Core.Editor;

namespace Unity.Services.Authentication.Editor
{
    /// <summary>
    /// Implementation of the <see cref="IEditorGameServiceIdentifier"/> for the Authentication package
    /// </summary>
    /// <remarks>This identifier MUST be public struct.</remarks>
    struct AuthenticationIdentifier : IEditorGameServiceIdentifier
    {
        /// <summary>
        /// Key for the Authentication package
        /// </summary>
        /// <returns>The identifier of the game service.</returns>
        public string GetKey() => "Authentication";
    }
}
