window.dashboardCharts = {
    chart: null,

    renderUsersChart: function (admins, users) {

        const canvas = document.getElementById("usersChart");

        if (!canvas) return;

        if (this.chart) {
            this.chart.destroy();
        }

        this.chart = new Chart(canvas, {
            type: "doughnut",

            data: {
                labels: ["Administradores", "Usuários"],

                datasets: [{
                    data: [admins, users],

                    backgroundColor: [
                        "#6f42c1",
                        "#0d6efd"
                    ],

                    borderWidth: 2
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,

                plugins: {
                    legend: {
                        position: "bottom"
                    }
                }
            }
        });
    }
};