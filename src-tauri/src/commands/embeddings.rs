use crate::db::{models::Embedding, open_connection};
use rusqlite::params;
use chrono::Utc;

#[tauri::command]
pub fn save_embedding(note_id: String, embedding: String, model: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let now = Utc::now().to_rfc3339();
    conn.execute(
        "INSERT INTO embeddings (note_id, embedding, model, updated_at) VALUES (?1, ?2, ?3, ?4)
         ON CONFLICT(note_id) DO UPDATE SET embedding = excluded.embedding, model = excluded.model, updated_at = excluded.updated_at",
        params![note_id, embedding, model, now],
    )
    .map_err(|e| e.to_string())?;
    Ok(())
}

#[tauri::command]
pub fn get_all_embeddings() -> Result<Vec<Embedding>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let mut stmt = conn
        .prepare("SELECT note_id, embedding, model, updated_at FROM embeddings")
        .map_err(|e| e.to_string())?;

    let embeddings = stmt
        .query_map([], |row| {
            Ok(Embedding {
                note_id: row.get(0)?,
                embedding: row.get(1)?,
                model: row.get(2)?,
                updated_at: row.get(3)?,
            })
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    Ok(embeddings)
}

#[tauri::command]
pub fn delete_embedding(note_id: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    conn.execute("DELETE FROM embeddings WHERE note_id = ?1", params![note_id])
        .map_err(|e| e.to_string())?;
    Ok(())
}
