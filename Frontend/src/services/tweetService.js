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