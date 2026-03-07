mod db;
mod commands;

use commands::{
    notebooks::{get_notebooks, create_notebook, rename_notebook, delete_notebook, reorder_notebooks},
    notes::{get_notes, get_all_notes, get_note, create_note, save_note, delete_note, move_note, search_notes},
    attachments::{get_attachments, get_all_attachments_meta, add_attachment, get_attachment_data, delete_attachment},
    embeddings::{save_embedding, get_all_embeddings, delete_embedding},
    preferences::{get_preference, set_preference, delete_preference},
    openai_admin::get_openai_usage,
};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    db::initialize().expect("Failed to initialize database");

    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_dialog::init())
        .invoke_handler(tauri::generate_handler![
            // Notebooks
            get_notebooks,
            create_notebook,
            rename_notebook,
            delete_notebook,
            reorder_notebooks,
            // Notes
            get_notes,
            get_all_notes,
            get_note,
            create_note,
            save_note,
            delete_note,
            move_note,
            search_notes,
            // Attachments
            get_attachments,
            get_all_attachments_meta,
            add_attachment,
            get_attachment_data,
            delete_attachment,
            // Embeddings
            save_embedding,
            get_all_embeddings,
            delete_embedding,
            // Preferences
            get_preference,
            set_preference,
            delete_preference,
            // OpenAI Admin
            get_openai_usage,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
