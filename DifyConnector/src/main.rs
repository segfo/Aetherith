use std::env;
use std::fs;
use dotenvy;

use reqwest::header::{AUTHORIZATION, CONTENT_TYPE};
use tokio_stream::StreamExt;

#[tokio::main]
async fn main() {
    dotenvy::dotenv().ok();
    let args: Vec<String> = env::args().collect();
    if args.len() < 4 && (env::var("DIFY_APIKEY").is_err() || env::var("DIFY_API_ENDPOINT").is_err() || env::var("QUERY_TEXT").is_err()) {
        eprintln!("Usage: cargo run -- <API_KEY> <ENDPOINT_URL> <QUERY_FILE>");
        std::process::exit(1);
    }
    let api_key = env::var("DIFY_APIKEY").unwrap_or_else(|_| args.get(1).expect("DIFY_APIKEY not set and no arg[1]").to_owned());
    let url     = env::var("DIFY_API_ENDPOINT").unwrap_or_else(|_| args.get(2).expect("DIFY_API_ENDPOINT not set and no arg[2]").to_owned());
    let query   = env::var("QUERY_TEXT").unwrap_or_else(|_| { fs::read_to_string(args.get(3).expect("QUERY file path not set")).expect("Failed to read query file")});
    // JSONリクエストボディ構築
    let json_body = serde_json::json!({
        "inputs": {},
        "query": query,
        "response_mode": "streaming",
        "conversation_id": "",
        "user": "AetherithDifyConnector"
    });

    let client = reqwest::Client::new();

    let response = client
        .post(url)
        .header(AUTHORIZATION, format!("Bearer {}", api_key))
        .header(CONTENT_TYPE, "application/json")
        .json(&json_body)
        .send()
        .await
        .expect("Failed to send request");

    let mut stream = response.bytes_stream();

    while let Some(item) = stream.next().await {
        match item {
            Ok(chunk) => {
                let text = String::from_utf8_lossy(&chunk);
                for line in text.lines() {
                    if line.starts_with("data: ") {
                        println!("{}", &line[6..]);
                    }
                }
            }
            Err(e) => {
                eprintln!("Stream error: {}", e);
                break;
            }
        }
    }
}