use crate::db::{models::{Block, Note}, open_connection};
use rusqlite::params;
use uuid::Uuid;
use chrono::Utc;
use serde::Deserialize;

#[derive(Debug, Deserialize)]
pub struct BlockInput {
    pub id: Option<String>,
    pub block_type: String,
    pub content: String,
    pub metadata: Option<String>,
    pub order: i32,
}

fn load_blocks(conn: &rusqlite::Connection, note_id: &str) -> Result<Vec<Block>, rusqlite::Error> {
    let mut stmt = conn.prepare(
        "SELECT id, note_id, block_type, content, metadata, \"order\" FROM blocks WHERE note_id = ?1 ORDER BY \"order\" ASC",
    )?;
    let blocks = stmt
        .query_map(params![note_id], |row| {
            Ok(Block {
                id: row.get(0)?,
                note_id: row.get(1)?,
                block_type: row.get(2)?,
                content: row.get(3)?,
                metadata: row.get(4)?,
                order: row.get(5)?,
            })
        })?
        .collect::<Result<Vec<_>, _>>()?;
    Ok(blocks)
}

#[tauri::command]
pub fn get_notes(notebook_id: String) -> Result<Vec<Note>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let mut stmt = conn
        .prepare(
            "SELECT id, notebook_id, created_at, last_modified_at FROM notes WHERE notebook_id = ?1 ORDER BY created_at DESC",
        )
        .map_err(|e| e.to_string())?;

    let notes_raw: Vec<(String, String, String, String)> = stmt
        .query_map(params![notebook_id], |row| {
            Ok((row.get(0)?, row.get(1)?, row.get(2)?, row.get(3)?))
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    let mut notes = Vec::new();
    for (id, nb_id, created_at, last_modified_at) in notes_raw {
        let blocks = load_blocks(&conn, &id).map_err(|e| e.to_string())?;
        notes.push(Note {
            id,
            notebook_id: nb_id,
            created_at,
            last_modified_at,
            blocks,
        });
    }

    Ok(notes)
}

#[tauri::command]
pub fn get_all_notes() -> Result<Vec<Note>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let mut stmt = conn
        .prepare(
            "SELECT id, notebook_id, created_at, last_modified_at FROM notes ORDER BY created_at DESC",
        )
        .map_err(|e| e.to_string())?;

    let notes_raw: Vec<(String, String, String, String)> = stmt
        .query_map([], |row| {
            Ok((row.get(0)?, row.get(1)?, row.get(2)?, row.get(3)?))
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    let mut notes = Vec::new();
    for (id, nb_id, created_at, last_modified_at) in notes_raw {
        let blocks = load_blocks(&conn, &id).map_err(|e| e.to_string())?;
        notes.push(Note {
            id,
            notebook_id: nb_id,
            created_at,
            last_modified_at,
            blocks,
        });
    }

    Ok(notes)
}

#[tauri::command]
pub fn get_note(note_id: String) -> Result<Option<Note>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let result = conn.query_row(
        "SELECT id, notebook_id, created_at, last_modified_at FROM notes WHERE id = ?1",
        params![note_id],
        |row| Ok((row.get::<_, String>(0)?, row.get::<_, String>(1)?, row.get::<_, String>(2)?, row.get::<_, String>(3)?)),
    );

    match result {
        Ok((id, notebook_id, created_at, last_modified_at)) => {
            let blocks = load_blocks(&conn, &id).map_err(|e| e.to_string())?;
            Ok(Some(Note { id, notebook_id, created_at, last_modified_at, blocks }))
        }
        Err(rusqlite::Error::QueryReturnedNoRows) => Ok(None),
        Err(e) => Err(e.to_string()),
    }
}

#[tauri::command]
pub fn create_note(notebook_id: String, blocks: Vec<BlockInput>) -> Result<Note, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let now = Utc::now().to_rfc3339();
    let note_id = Uuid::new_v4().to_string();

    conn.execute(
        "INSERT INTO notes (id, notebook_id, created_at, last_modified_at) VALUES (?1, ?2, ?3, ?3)",
        params![note_id, notebook_id, now],
    )
    .map_err(|e| e.to_string())?;

    let mut created_blocks = Vec::new();
    for block_input in blocks {
        let block_id = block_input.id.unwrap_or_else(|| Uuid::new_v4().to_string());
        let metadata = block_input.metadata.unwrap_or_else(|| "{}".to_string());
        conn.execute(
            "INSERT INTO blocks (id, note_id, block_type, content, metadata, \"order\") VALUES (?1, ?2, ?3, ?4, ?5, ?6)",
            params![block_id, note_id, block_input.block_type, block_input.content, metadata, block_input.order],
        )
        .map_err(|e| e.to_string())?;
        created_blocks.push(Block {
            id: block_id,
            note_id: note_id.clone(),
            block_type: block_input.block_type,
            content: block_input.content,
            metadata,
            order: block_input.order,
        });
    }

    Ok(Note {
        id: note_id,
        notebook_id,
        created_at: now.clone(),
        last_modified_at: now,
        blocks: created_blocks,
    })
}

#[tauri::command]
pub fn save_note(note_id: String, blocks: Vec<BlockInput>) -> Result<Note, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let now = Utc::now().to_rfc3339();

    // Get existing note metadata
    let (notebook_id, created_at): (String, String) = conn
        .query_row(
            "SELECT notebook_id, created_at FROM notes WHERE id = ?1",
            params![note_id],
            |row| Ok((row.get(0)?, row.get(1)?)),
        )
        .map_err(|e| e.to_string())?;

    conn.execute(
        "UPDATE notes SET last_modified_at = ?1 WHERE id = ?2",
        params![now, note_id],
    )
    .map_err(|e| e.to_string())?;

    // Delete existing blocks and re-insert
    conn.execute("DELETE FROM blocks WHERE note_id = ?1", params![note_id])
        .map_err(|e| e.to_string())?;

    let mut saved_blocks = Vec::new();
    for block_input in blocks {
        let block_id = block_input.id.unwrap_or_else(|| Uuid::new_v4().to_string());
        let metadata = block_input.metadata.unwrap_or_else(|| "{}".to_string());
        conn.execute(
            "INSERT INTO blocks (id, note_id, block_type, content, metadata, \"order\") VALUES (?1, ?2, ?3, ?4, ?5, ?6)",
            params![block_id, note_id, block_input.block_type, block_input.content, metadata, block_input.order],
        )
        .map_err(|e| e.to_string())?;
        saved_blocks.push(Block {
            id: block_id,
            note_id: note_id.clone(),
            block_type: block_input.block_type,
            content: block_input.content,
            metadata,
            order: block_input.order,
        });
    }

    Ok(Note {
        id: note_id,
        notebook_id,
        created_at,
        last_modified_at: now,
        blocks: saved_blocks,
    })
}

#[tauri::command]
pub fn delete_note(note_id: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    conn.execute("DELETE FROM notes WHERE id = ?1", params![note_id])
        .map_err(|e| e.to_string())?;
    Ok(())
}

#[tauri::command]
pub fn move_note(note_id: String, notebook_id: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let now = Utc::now().to_rfc3339();
    conn.execute(
        "UPDATE notes SET notebook_id = ?1, last_modified_at = ?2 WHERE id = ?3",
        params![notebook_id, now, note_id],
    )
    .map_err(|e| e.to_string())?;
    Ok(())
}

#[tauri::command]
pub fn search_notes(query: String) -> Result<Vec<Note>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let pattern = format!("%{}%", query.to_lowercase());

    let mut stmt = conn
        .prepare(
            "SELECT DISTINCT n.id, n.notebook_id, n.created_at, n.last_modified_at
             FROM notes n
             JOIN blocks b ON b.note_id = n.id
             WHERE LOWER(b.content) LIKE ?1
             ORDER BY n.created_at DESC
             LIMIT 50",
        )
        .map_err(|e| e.to_string())?;

    let notes_raw: Vec<(String, String, String, String)> = stmt
        .query_map(params![pattern], |row| {
            Ok((row.get(0)?, row.get(1)?, row.get(2)?, row.get(3)?))
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    let mut notes = Vec::new();
    for (id, nb_id, created_at, last_modified_at) in notes_raw {
        let blocks = load_blocks(&conn, &id).map_err(|e| e.to_string())?;
        notes.push(Note { id, notebook_id: nb_id, created_at, last_modified_at, blocks });
    }

    Ok(notes)
}
