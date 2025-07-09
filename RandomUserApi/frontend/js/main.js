let usersData = [];
let currentPage = 1;
const itemsPerPage = 10;

// Sayfa yüklendiğinde verileri çek
document.addEventListener("DOMContentLoaded", () => {
  fetchUsers();
});

// Kullanıcıları getir
async function fetchUsers() {
  try {
    const response = await fetch("http://localhost:5073/api/users", {
      method: "GET",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      mode: "cors",
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    usersData = data.results;
    renderTable();
    renderPageNumbers();
  } catch (error) {
    console.error("Veriler çekilirken hata oluştu:", error);
    alert("Veriler yüklenirken bir hata oluştu! Hata detayı: " + error.message);
  }
}

function renderTable() {
  const tbody = document.getElementById("userTableBody");
  tbody.innerHTML = "";

  const start = (currentPage - 1) * itemsPerPage;
  const end = start + itemsPerPage;
  const paginatedUsers = usersData.slice(start, end);

  paginatedUsers.forEach((user) => {
    const row = `<tr>
          <td>${user.login?.username || ""}</td>
          <td>${user.name?.first || ""}</td>
          <td>${user.name?.last || ""}</td>
          <td>${user.email || ""}</td>
          <td>${user.phone || ""}</td>
          <td>
            <button class="btn btn-primary btn-sm me-2" onclick="editUser('${
              user.login?.uuid
            }')">Düzenle</button>
            <button class="btn btn-danger btn-sm" onclick="deleteUser('${
              user.login?.uuid
            }')">Sil</button>
          </td>
        </tr>`;
    tbody.insertAdjacentHTML("beforeend", row);
  });
}

function renderPageNumbers() {
  const totalPages = Math.ceil(usersData.length / itemsPerPage);
  const pageNumbersContainer = document.getElementById("pageNumbers");
  pageNumbersContainer.innerHTML = "";

  const range = 2;
  const startPage = Math.max(1, currentPage - range);
  const endPage = Math.min(totalPages, currentPage + range);

  for (let i = startPage; i <= endPage; i++) {
    const btn = document.createElement("button");
    btn.textContent = i;
    btn.className = i === currentPage ? "active-page" : "";
    btn.onclick = () => {
      currentPage = i;
      renderTable();
      renderPageNumbers();
    };
    pageNumbersContainer.appendChild(btn);
  }

  document.getElementById("prevBtn").disabled = currentPage === 1;
  document.getElementById("nextBtn").disabled = currentPage === totalPages;
}

function previousPage() {
  if (currentPage > 1) {
    currentPage--;
    renderTable();
    renderPageNumbers();
  }
}

function nextPage() {
  const totalPages = Math.ceil(usersData.length / itemsPerPage);
  if (currentPage < totalPages) {
    currentPage++;
    renderTable();
    renderPageNumbers();
  }
}

async function editUser(uuid) {
  try {
    const response = await fetch(`http://localhost:5073/api/users/${uuid}`, {
      method: "GET",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      mode: "cors",
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const user = await response.json();

    document.getElementById("editUuid").value = uuid;
    document.getElementById("editUsername").value = user.login?.username || "";
    document.getElementById("editFirstName").value = user.name?.first || "";
    document.getElementById("editLastName").value = user.name?.last || "";
    document.getElementById("editEmail").value = user.email || "";
    document.getElementById("editPhone").value = user.phone || "";
    document.getElementById("editGender").value = user.gender || "";

    const modal = new bootstrap.Modal(document.getElementById("editModal"));
    modal.show();
  } catch (error) {
    console.error("Kullanıcı bilgileri alınırken hata:", error);
    alert("Kullanıcı bilgileri alınırken bir hata oluştu!");
  }
}

async function saveUser() {
  const uuid = document.getElementById("editUuid").value;
  const userData = {
    gender: document.getElementById("editGender").value,
    name: {
      first: document.getElementById("editFirstName").value,
      last: document.getElementById("editLastName").value,
    },
    username: document.getElementById("editUsername").value,
    email: document.getElementById("editEmail").value,
    phone: document.getElementById("editPhone").value,
  };

  try {
    const response = await fetch(`http://localhost:5073/api/users/${uuid}`, {
      method: "PUT",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(userData),
      mode: "cors",
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const modal = bootstrap.Modal.getInstance(
      document.getElementById("editModal")
    );
    modal.hide();

    fetchUsers();
    alert("Kullanıcı başarıyla güncellendi!");
  } catch (error) {
    console.error("Kullanıcı güncellenirken hata:", error);
    alert("Kullanıcı güncellenirken bir hata oluştu!", error.message);
  }
}

async function deleteUser(uuid) {
  if (confirm("Bu kullanıcıyı silmek istediğinizden emin misiniz?")) {
    try {
      const response = await fetch(`http://localhost:5073/api/users/${uuid}`, {
        method: "DELETE",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        mode: "cors",
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      fetchUsers();
      alert("Kullanıcı başarıyla silindi!");
    } catch (error) {
      console.error("Kullanıcı silinirken hata:", error);
      alert("Kullanıcı silinirken bir hata oluştu!");
    }
  }
}

async function addUser() {
  const userData = {
    gender: document.getElementById("addGender").value,
    name: {
      first: document.getElementById("addFirstName").value,
      last: document.getElementById("addLastName").value,
    },
    username: document.getElementById("addUsername").value,
    email: document.getElementById("addEmail").value,
    phone: document.getElementById("addPhone").value,
  };
  try {
    const response = await fetch("http://localhost:5073/api/users/add", {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(userData),
      mode: "cors",
    });
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    const result = await response.json();
    const modal = bootstrap.Modal.getInstance(
      document.getElementById("addUserModal")
    );
    modal.hide();

    fetchUsers();
    alert("Yeni kullanıcı başarıyla eklendi!");
  } catch (error) {
    console.error("Yeni kullanıcı eklenirken hata oluştu:", error);
    alert("Yeni kullanıcı eklenirken bir hata oluştu!");
  }
}
