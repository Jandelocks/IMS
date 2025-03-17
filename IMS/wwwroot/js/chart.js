document.addEventListener("DOMContentLoaded", function () {
    // Default filters
    let incidentFilter = "monthly";
    let userRoleFilter = "monthly";

    // Initialize Charts
    let incidentChartCtx = document.getElementById("incidentChart").getContext("2d");
    let userRoleChartCtx = document.getElementById("userRoleChart").getContext("2d");

    let incidentChart = new Chart(incidentChartCtx, {
        type: "bar",
        data: { labels: [], datasets: [{ label: "Incidents", data: [], backgroundColor: "rgba(255, 99, 132, 0.5)" }] },
        options: { responsive: true }
    });

    let userRoleChart = new Chart(userRoleChartCtx, {
        type: "pie",
        data: { labels: [], datasets: [{ label: "Users", data: [], backgroundColor: ["#ff6384", "#36a2eb", "#ffce56"] }] },
        options: { responsive: true }
    });

    // Fetch Data Functions
    function fetchIncidentData() {
        fetch(`/Charts/GetIncidentData?filter=${incidentFilter}`)
            .then(response => response.json())
            .then(data => {
                incidentChart.data.labels = data.map(d => d.category);
                incidentChart.data.datasets[0].data = data.map(d => d.count);
                incidentChart.update();
            });
    }

    function fetchUserRoleData() {
        fetch(`/Charts/GetUserRoleData?filter=${userRoleFilter}`)
            .then(response => response.json())
            .then(data => {
                userRoleChart.data.labels = data.map(d => d.role);
                userRoleChart.data.datasets[0].data = data.map(d => d.count);
                userRoleChart.update();
            });
    }

    // Event Listeners for Buttons
    document.querySelectorAll(".incident-filter").forEach(button => {
        button.addEventListener("click", function () {
            incidentFilter = this.getAttribute("data-filter");
            fetchIncidentData();
        });
    });

    document.querySelectorAll(".user-role-filter").forEach(button => {
        button.addEventListener("click", function () {
            userRoleFilter = this.getAttribute("data-filter");
            fetchUserRoleData();
        });
    });

    // Load initial data
    fetchIncidentData();
    fetchUserRoleData();
});



document.addEventListener("DOMContentLoaded", function () {
    const userId = document.getElementById("moderatorChart").getAttribute("data-userid");

    fetch(`/Charts/GetModeratorIncidentChart?userId=${userId}`)
        .then(response => response.json())
        .then(data => {
            const ctx = document.getElementById("moderatorChart").getContext("2d");

            new Chart(ctx, {
                type: "bar",
                data: {
                    labels: ["Daily", "Monthly", "Yearly"],
                    datasets: [{
                        label: "Incidents Assigned",
                        data: [data.daily, data.monthly, data.yearly],
                        backgroundColor: ["#ff6384", "#36a2eb", "#ffce56"],
                        borderColor: ["#cc2e49", "#287dbd", "#d1a330"],
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    scales: {
                        y: { beginAtZero: true }
                    }
                }
            });
        })
        .catch(error => console.error("Error loading chart data:", error));
});


document.addEventListener("DOMContentLoaded", function () {
    let chartInstance;
    let ctx = document.getElementById("incidentChartcount").getContext("2d");

    fetch("/Charts/GetIncidentCounts")
        .then(response => response.json())
        .then(data => {
            let datasets = {
                daily: {
                    labels: data.daily.map(d => moment(d.date).format("MMM D, YYYY")),
                    counts: data.daily.map(d => d.count)
                },
                monthly: {
                    labels: data.monthly.map(m => moment(`${m.year}-${m.month}-01`).format("MMM YYYY")),
                    counts: data.monthly.map(m => m.count)
                },
                yearly: {
                    labels: data.yearly.map(y => y.year.toString()),
                    counts: data.yearly.map(y => y.count)
                }
            };

            function updateChart(type) {
                if (chartInstance) chartInstance.destroy();

                chartInstance = new Chart(ctx, {
                    type: "bar",
                    data: {
                        labels: datasets[type].labels,
                        datasets: [{
                            label: `Incidents (${type.charAt(0).toUpperCase() + type.slice(1)})`,
                            data: datasets[type].counts,
                            backgroundColor: type === "daily" ? "#4bc0c0" : type === "monthly" ? "#36a2eb" : "#ffce56",
                            borderColor: type === "daily" ? "#4bc0c0" : type === "monthly" ? "#36a2eb" : "#ffce56",
                            borderWidth: 1
                        }]
                    }
                });
            }

            // Default chart (daily)
            updateChart("daily");

            // Button click event listener
            document.querySelectorAll(".total-incidents").forEach(btn => {
                btn.addEventListener("click", function () {
                    updateChart(this.getAttribute("data-filter"));
                });
            });
        });
});