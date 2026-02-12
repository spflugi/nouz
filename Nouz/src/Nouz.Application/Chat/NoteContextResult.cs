using Nouz.Domain.Entities;

namespace Nouz.Application.Chat;

/// <summary>
/// Represents a note that was retrieved as context for a chat query, along with its similarity score and title.
/// </summary>
public sealed record NoteContextResult(Note Note, string Title, float Similarity);
