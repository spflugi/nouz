use crate::db::{models::{Attachment, AttachmentMeta}, open_connection};
use rusqlite::params;
use uuid::Uuid;
use chrono::Utc;

#[tauri::command]
pub fn get_attachments(note_id: String) -> Result<Vec<AttachmentMeta>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let mut stmt = conn
        .prepare(
            "SELECT id, note_id, file_name, mime_type, created_at FROM attachments WHERE note_id = ?1 ORDER BY created_at ASC",
        )
        .map_err(|e| e.to_string())?;

    let attachments = stmt
        .query_map(params![note_id], |row| {
            Ok(AttachmentMeta {
                id: row.get(0)?,
                note_id: row.get(1)?,
                file_name: row.get(2)?,
                mime_type: row.get(3)?,
                created_at: row.get(4)?,
            })
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    Ok(attachments)
}

#[tauri::command]
pub fn get_all_attachments_meta() -> Result<Vec<AttachmentMeta>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let mut stmt = conn
        .prepare(
            "SELECT id, note_id, file_name, mime_type, created_at FROM attachments ORDER BY created_at ASC",
        )
        .map_err(|e| e.to_string())?;

    let attachments = stmt
        .query_map([], |row| {
            Ok(AttachmentMeta {
                id: row.get(0)?,
                note_id: row.get(1)?,
                file_name: row.get(2)?,
                mime_type: row.get(3)?,
                created_at: row.get(4)?,
            })
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    Ok(attachments)
}

#[tauri::command]
pub fn add_attachment(
    note_id: String,
    file_name: String,
    mime_type: String,
    data: Vec<u8>,
) -> Result<AttachmentMeta, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let id = Uuid::new_v4().to_string();
    let now = Utc::now().to_rfc3339();

    conn.execute(
        "INSERT INTO attachments (id, note_id, file_name, mime_type, data, created_at) VALUES (?1, ?2, ?3, ?4, ?5, ?6)",
        params![id, note_id, file_name, mime_type, data, now],
    )
    .map_err(|e| e.to_string())?;

    Ok(AttachmentMeta {
        id,
        note_id,
        file_name,
        mime_type,
        created_at: now,
    })
}

#[tauri::command]
pub fn get_attachment_data(attachment_id: String) -> Result<Attachment, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    conn.query_row(
        "SELECT id, note_id, file_name, mime_type, data, created_at FROM attachments WHERE id = ?1",
        params![attachment_id],
        |row| {
            Ok(Attachment {
                id: row.get(0)?,
                note_id: row.get(1)?,
                file_name: row.get(2)?,
                mime_type: row.get(3)?,
                data: row.get(4)?,
                created_at: row.get(5)?,
            })
        },
    )
    .map_err(|e| e.to_string())
}

#[tauri::command]
pub fn delete_attachment(attachment_id: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    conn.execute(
        "DELETE FROM attachments WHERE id = ?1",
        params![attachment_id],
    )
    .map_err(|e| e.to_string())?;
    Ok(())
}
