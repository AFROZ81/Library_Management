# 📚 LibraryPro: Digital Collection Archive

**LibraryPro** is a high-performance, responsive Digital Catalog Management System designed to handle modern library volumes with a focus on user experience and real-time inventory tracking. 

The application is built using a **"Fixed Viewport Architecture"**, creating a professional desktop-app feel where the navigation and pagination are always locked in view, while the collection scrolls independently.

---

## 🚀 Key Features

### 1. Compact Viewport Design
Unlike traditional websites, LibraryPro utilizes a **CSS-driven layout** that eliminates body scrolling. 
- **Sticky Controls:** The header (search/sort) and footer (pagination) never leave the screen.
- **Scrollable Archive:** Only the book grid container scrolls, ensuring you never lose your place.

### 2. Intelligent Search & Sort
- **Global Search:** Query by Title, Author, or ISBN via a modern glassmorphism search bar.
- **Multi-State Sorting:** Toggle between Title, Author, and Year of Publication.
- **State Persistence:** Search filters and sort orders are preserved during pagination transitions.

### 3. Dynamic Inventory Visualization
Each volume in the archive is represented by a high-info card:
- **Real-time Status:** LED-style status dots (Green for Available, Red for Out of Stock).
- **Inventory Progress:** A dynamic progress bar that changes color (Blue/Yellow/Red) based on the stock percentage.
- **Quick Actions:** Instant access to Edit and Delete (powered by SweetAlert2) without leaving the catalog.

### 4. Advanced Pagination
- **Server-Side Processing:** Data is paginated at the database level using `IQueryable` for maximum speed.
- **Responsive Paging:** Controls adapt for mobile view to show "Page X of Y" instead of long numeric lists.

---

## 🛠 Tech Stack

| Layer | Technology |
| :--- | :--- |
| **Backend** | ASP.NET Core MVC 8.0 |
| **ORM** | Entity Framework Core |
| **Database** | Microsoft SQL Server |
| **Frontend** | Razor Pages (C#), Bootstrap 5 |
| **Scripts** | JavaScript ES6, SweetAlert2 |
| **Icons** | Bootstrap Icons |

---

## 📂 Project Architecture



```text
LibraryPro/
├── Models/
│   ├── Entities/          # Book and Genre Domain Models
│   └── PaginatedList.cs   # Generic Logic for Database Pagination
├── Controllers/
│   └── BooksController.cs # Handles Search, Sort, and CRUD logic
├── Views/
│   └── Books/
│       ├── Index.cshtml   # The Main Digital Catalog (Viewport UI)
│       ├── Create.cshtml  # Volume Entry Form
│       └── Edit.cshtml    # Volume Modification Form
└── wwwroot/
    └── css/               # Custom Viewport & Gradient Styling
