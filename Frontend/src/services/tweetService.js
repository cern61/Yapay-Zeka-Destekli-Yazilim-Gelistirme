const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export async function createTweet(content, parentId = null) {
    const token = sessionStorage.getItem("authToken");

    if (!token) {
        throw new Error("Gönderi paylaşmak için giriş yapmalısınız.");
    }

    if (!API_BASE_URL) {
        throw new Error("API adresi tanımlanmamış.");
    }

    const payload = { content: content.trim() };

    if (parentId) {
        payload.parentId = parentId;
    }

    let response;

    try {
        response = await fetch(`${API_BASE_URL.replace(/\/$/, "")}/api/tweets`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Accept": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });
    } catch {
        throw new Error("Sunucuya bağlanılamadı.");
    }

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        const validationErrors = data?.errors
            ? Object.values(data.errors).flat().join(" ")
            : "";

        throw new Error(
            validationErrors ||
            data?.message ||
            data?.detail ||
            data?.title ||
            `Gönderi oluşturulamadı. HTTP ${response.status}`
        );
    }

    return data;
}


async function sendTweetAction(tweetId, method) {
    const token = sessionStorage.getItem("authToken");

    if (!token) {
        throw new Error("Bu işlem için giriş yapmalısınız.");
    }

    if (!API_BASE_URL) {
        throw new Error("API adresi tanımlanmamış.");
    }

    if (!tweetId) {
        throw new Error("Gönderi ID'si bulunamadı.");
    }

    let response;

    try {
        response = await fetch(
            `${API_BASE_URL.replace(/\/$/, "")}/api/tweets/${encodeURIComponent(tweetId)}/like`,
            {
                method,
                headers: {
                    "Accept": "application/json",
                    "Authorization": `Bearer ${token}`
                }
            }
        );
    } catch {
        throw new Error("Sunucuya bağlanılamadı.");
    }

    if (!response.ok) {
        const data = await response.json().catch(() => null);

        throw new Error(
            data?.message ||
            data?.detail ||
            data?.title ||
            `Beğeni işlemi başarısız. HTTP ${response.status}`
        );
    }

    return true;
}

export function likeTweet(tweetId) {
    return sendTweetAction(tweetId, "PUT");
}

export function unlikeTweet(tweetId) {
    return sendTweetAction(tweetId, "DELETE");
}


export async function getTweetReplies(tweetId, cursor = null) {
    if (!API_BASE_URL) {
        throw new Error("API adresi tanımlanmamış.");
    }

    if (!tweetId) {
        throw new Error("Gönderi ID'si bulunamadı.");
    }

    const url = new URL(
        `/api/tweets/${encodeURIComponent(tweetId)}/replies`,
        API_BASE_URL
    );

    if (cursor) {
        url.searchParams.set("cursor", cursor);
    }

    const token = sessionStorage.getItem("authToken");

    const headers = {
        "Accept": "application/json"
    };

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    let response;

    try {
        response = await fetch(url, {
            method: "GET",
            headers
        });
    } catch {
        throw new Error("Yanıtlar alınırken sunucuya bağlanılamadı.");
    }

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        throw new Error(
            data?.message ||
            data?.detail ||
            data?.title ||
            `Yanıtlar alınamadı. HTTP ${response.status}`
        );
    }

    if (!Array.isArray(data?.items)) {
        throw new Error("API yanıt formatı beklenen yapıda değil.");
    }

    return data;
}


export async function deleteTweet(tweetId) {
    const token = sessionStorage.getItem("authToken");

    if (!token) {
        throw new Error("Gönderi silmek için giriş yapmalısınız.");
    }

    if (!API_BASE_URL) {
        throw new Error("API adresi tanımlanmamış.");
    }

    if (!tweetId) {
        throw new Error("Gönderi ID'si bulunamadı.");
    }

    let response;

    try {
        response = await fetch(
            `${API_BASE_URL.replace(/\/$/, "")}/api/tweets/${encodeURIComponent(tweetId)}`,
            {
                method: "DELETE",
                headers: {
                    "Accept": "application/json",
                    "Authorization": `Bearer ${token}`
                }
            }
        );
    } catch {
        throw new Error("Sunucuya bağlanılamadı.");
    }

    if (!response.ok) {
        const data = await response.json().catch(() => null);

        throw new Error(
            data?.message ||
            data?.detail ||
            data?.title ||
            `Gönderi silinemedi. HTTP ${response.status}`
        );
    }

    return true;
}
