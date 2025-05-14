// Sayfa yüklendiğinde verileri çek
document.addEventListener('DOMContentLoaded', () => {
    fetchUsers();
});

// Kullanıcıları getir
async function fetchUsers() {
    try {
        console.log('Veriler çekiliyor...');
        const response = await fetch('http://localhost:5073/api/users', {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            mode: 'cors' // CORS modunu açıkça belirt
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        console.log('Gelen veri:', data);
        displayUsers(data.results);
    } catch (error) {
        console.error('Veriler çekilirken hata oluştu:', error);
        alert('Veriler yüklenirken bir hata oluştu! Hata detayı: ' + error.message);
    }
}

// Kullanıcıları tabloya ekle
function displayUsers(users) {
    const tableBody = document.getElementById('userTableBody');
    tableBody.innerHTML = '';

    users.forEach(user => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${user.login?.uuid || ''}</td>
            <td>${user.name?.first || ''}</td>
            <td>${user.name?.last || ''}</td>
            <td>${user.email || ''}</td>
            <td>${user.phone || ''}</td>
            <td>
                <button class="btn btn-sm btn-primary" onclick="editUser('${user.login?.uuid}')">Düzenle</button>
                <button class="btn btn-sm btn-danger" onclick="deleteUser('${user.login?.uuid}')">Sil</button>
            </td>
        `;
        tableBody.appendChild(row);
    });
}

// Kullanıcı düzenleme fonksiyonu
async function editUser(uuid) {
    try {
        const response = await fetch(`http://localhost:5073/api/users/${uuid}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            mode: 'cors'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const user = await response.json();
        
        // Modal alanlarını doldur
        document.getElementById('editUuid').value = uuid;
        document.getElementById('editTitle').value = user.name?.title || '';
        document.getElementById('editFirstName').value = user.name?.first || '';
        document.getElementById('editLastName').value = user.name?.last || '';
        document.getElementById('editEmail').value = user.email || '';
        document.getElementById('editPhone').value = user.phone || '';
        document.getElementById('editGender').value = user.gender || '';

        // Modalı göster
        const modal = new bootstrap.Modal(document.getElementById('editModal'));
        modal.show();
    } catch (error) {
        console.error('Kullanıcı bilgileri alınırken hata:', error);
        alert('Kullanıcı bilgileri alınırken bir hata oluştu!');
    }
}

// Kullanıcı kaydetme fonksiyonu
async function saveUser() {
    const uuid = document.getElementById('editUuid').value;
    const userData = {
        gender: document.getElementById('editGender').value,
        name: {
            title: document.getElementById('editTitle').value,
            first: document.getElementById('editFirstName').value,
            last: document.getElementById('editLastName').value
        },
        email: document.getElementById('editEmail').value,
        phone: document.getElementById('editPhone').value
    };

    try {
        const response = await fetch(`http://localhost:5073/api/users/${uuid}`, {
            method: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userData),
            mode: 'cors'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // Modalı kapat
        const modal = bootstrap.Modal.getInstance(document.getElementById('editModal'));
        modal.hide();

        // Tabloyu yenile
        fetchUsers();
        
        alert('Kullanıcı başarıyla güncellendi!');
    } catch (error) {
        console.error('Kullanıcı güncellenirken hata:', error);
        alert('Kullanıcı güncellenirken bir hata oluştu!');
    }
}

// Kullanıcı silme fonksiyonu
async function deleteUser(uuid) {
    if (confirm('Bu kullanıcıyı silmek istediğinizden emin misiniz?')) {
        try {
            const response = await fetch(`http://localhost:5073/api/users/${uuid}`, {
                method: 'DELETE',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                mode: 'cors'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Tabloyu yenile
            fetchUsers();
            alert('Kullanıcı başarıyla silindi!');
        } catch (error) {
            console.error('Kullanıcı silinirken hata:', error);
            alert('Kullanıcı silinirken bir hata oluştu!');
        }
    }
} 