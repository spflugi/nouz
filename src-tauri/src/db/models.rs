use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Notebook {
    pub id: String,
    pub name: String,
    pub sort_order: i32,
    pub created_at: String,
    pub last_modified_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Note {
    pub id: String,
    pub notebook_id: String,
    pub created_at: String,
    pub last_modified_at: String,
    pub blocks: Vec<Block>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Block {
    pub id: String,
    pub note_id: String,
    pub block_type: String,
    pub content: String,
    pub metadata: String, // JSON string
    pub order: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Attachment {
    pub id: String,
    pub note_id: String,
    pub file_name: String,
    pub mime_type: String,
    pub data: Vec<u8>,
    pub created_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AttachmentMeta {
    pub id: String,
    pub note_id: String,
    pub file_name: String,
    pub mime_type: String,
    pub created_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Embedding {
    pub note_id: String,
    pub embedding: String, // JSON array of floats
    pub model: String,
    pub updated_at: String,
}
