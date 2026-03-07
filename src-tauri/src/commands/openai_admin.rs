use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct OpenAiUsage {
    pub cost: f64,
    pub input_tokens: i64,
    pub output_tokens: i64,
    pub requests: i64,
}

#[tauri::command]
pub async fn get_openai_usage(
    admin_key: String,
    start_time: i64,
    end_time: i64,
) -> Result<OpenAiUsage, String> {
    let client = reqwest::Client::new();
    let qs = format!("start_time={}&end_time={}&bucket_width=1d", start_time, end_time);
    let auth = format!("Bearer {}", admin_key);

    let costs_res = client
        .get(format!("https://api.openai.com/v1/organization/costs?{}", qs))
        .header("Authorization", &auth)
        .send()
        .await
        .map_err(|e| e.to_string())?;

    if !costs_res.status().is_success() {
        let status = costs_res.status();
        let body = costs_res.text().await.unwrap_or_default();
        return Err(format!("Costs API {}: {}", status.as_u16(), body));
    }

    let usage_res = client
        .get(format!(
            "https://api.openai.com/v1/organization/usage/completions?{}",
            qs
        ))
        .header("Authorization", &auth)
        .send()
        .await
        .map_err(|e| e.to_string())?;

    if !usage_res.status().is_success() {
        let status = usage_res.status();
        let body = usage_res.text().await.unwrap_or_default();
        return Err(format!("Usage API {}: {}", status.as_u16(), body));
    }

    let costs_json: serde_json::Value = costs_res.json().await.map_err(|e| e.to_string())?;
    let usage_json: serde_json::Value = usage_res.json().await.map_err(|e| e.to_string())?;

    let mut cost = 0.0_f64;
    if let Some(buckets) = costs_json["data"].as_array() {
        for bucket in buckets {
            if let Some(results) = bucket["results"].as_array() {
                for r in results {
                    cost += r["amount"]["value"].as_f64().unwrap_or(0.0);
                }
            }
        }
    }

    let mut input_tokens = 0i64;
    let mut output_tokens = 0i64;
    let mut requests = 0i64;
    if let Some(buckets) = usage_json["data"].as_array() {
        for bucket in buckets {
            if let Some(results) = bucket["results"].as_array() {
                for r in results {
                    input_tokens += r["input_tokens"].as_i64().unwrap_or(0);
                    output_tokens += r["output_tokens"].as_i64().unwrap_or(0);
                    requests += r["num_model_requests"].as_i64().unwrap_or(0);
                }
            }
        }
    }

    Ok(OpenAiUsage {
        cost,
        input_tokens,
        output_tokens,
        requests,
    })
}
