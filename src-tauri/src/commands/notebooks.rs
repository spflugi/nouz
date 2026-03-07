use crate::db::{models::Notebook, open_connection};
use rusqlite::params;
use uuid::Uuid;
use chrono::Utc;

#[tauri::command]
pub fn get_notebooks() -> Result<Vec<Notebook>, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let mut stmt = conn
        .prepare(
            "SELECT id, name, sort_order, created_at, last_modified_at FROM notebooks ORDER BY sort_order ASC, created_at ASC",
        )
        .map_err(|e| e.to_string())?;

    let notebooks = stmt
        .query_map([], |row| {
            Ok(Notebook {
                id: row.get(0)?,
                name: row.get(1)?,
                sort_order: row.get(2)?,
                created_at: row.get(3)?,
                last_modified_at: row.get(4)?,
            })
        })
        .map_err(|e| e.to_string())?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|e| e.to_string())?;

    Ok(notebooks)
}

#[tauri::command]
pub fn create_notebook(name: String) -> Result<Notebook, String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let now = Utc::now().to_rfc3339();
    let id = Uuid::new_v4().to_string();

    let sort_order: i32 = conn
        .query_row(
            "SELECT COALESCE(MAX(sort_order), -1) + 1 FROM notebooks",
            [],
            |row| row.get(0),
        )
        .unwrap_or(0);

    conn.execute(
        "INSERT INTO notebooks (id, name, sort_order, created_at, last_modified_at) VALUES (?1, ?2, ?3, ?4, ?4)",
        params![id, name, sort_order, now],
    )
    .map_err(|e| e.to_string())?;

    Ok(Notebook {
        id,
        name,
        sort_order,
        created_at: now.clone(),
        last_modified_at: now,
    })
}

#[tauri::command]
pub fn rename_notebook(id: String, name: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    let now = Utc::now().to_rfc3339();
    conn.execute(
        "UPDATE notebooks SET name = ?1, last_modified_at = ?2 WHERE id = ?3",
        params![name, now, id],
    )
    .map_err(|e| e.to_string())?;
    Ok(())
}

#[tauri::command]
pub fn delete_notebook(id: String) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    conn.execute("DELETE FROM notebooks WHERE id = ?1", params![id])
        .map_err(|e| e.to_string())?;
    Ok(())
}

#[tauri::command]
pub fn reorder_notebooks(ids: Vec<String>) -> Result<(), String> {
    let conn = open_connection().map_err(|e| e.to_string())?;
    for (i, id) in ids.iter().enumerate() {
        conn.execute(
            "UPDATE notebooks SET sort_order = ?1 WHERE id = ?2",
            params![i as i32, id],
        )
        .map_err(|e| e.to_string())?;
    }
    Ok(())
}
