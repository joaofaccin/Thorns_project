window.thornsTheme = (() => {
    const storageKey = "thorns-theme";
    const themes = new Set(["light", "dark"]);

    function normalizeTheme(theme) {
        return themes.has(theme) ? theme : "light";
    }

    function applyTheme(theme) {
        const selectedTheme = normalizeTheme(theme);
        document.documentElement.setAttribute("data-bs-theme", selectedTheme);
        return selectedTheme;
    }

    return {
        initialize() {
            const savedTheme = localStorage.getItem(storageKey);
            applyTheme(savedTheme);
        },

        getTheme() {
            return normalizeTheme(
                document.documentElement.getAttribute("data-bs-theme")
            );
        },

        setTheme(theme) {
            const selectedTheme = applyTheme(theme);
            localStorage.setItem(storageKey, selectedTheme);
        }
    };
})();
