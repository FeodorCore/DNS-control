-- Создание таблиц для базы данных "Учёт поставок и продаж"

-- Таблица категорий товаров
CREATE TABLE IF NOT EXISTS category (
    category_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL
);

-- Таблица поставщиков
CREATE TABLE IF NOT EXISTS supplier (
    supplier_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL,
    phone       TEXT,
    email       TEXT
);

-- Таблица товаров
CREATE TABLE IF NOT EXISTS product (
    product_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name          TEXT           NOT NULL,
    description   TEXT,
    current_price DECIMAL(10,2) NOT NULL,
    stock_quantity INT          NOT NULL,
    category_id   INT           NOT NULL REFERENCES category(category_id)
);

-- Таблица поставок (заголовок)
CREATE TABLE IF NOT EXISTS supply (
    supply_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    supplier_id INT           NOT NULL REFERENCES supplier(supplier_id),
    supply_date DATE          NOT NULL,
    total_cost  DECIMAL(12,2) NOT NULL
);

-- Таблица позиций поставки
CREATE TABLE IF NOT EXISTS supply_item (
    supply_item_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    supply_id           INT           NOT NULL REFERENCES supply(supply_id),
    product_id          INT           NOT NULL REFERENCES product(product_id),
    quantity            INT           NOT NULL,
    unit_purchase_price DECIMAL(10,2) NOT NULL
);

-- Таблица продаж (чек)
CREATE TABLE IF NOT EXISTS sale (
    sale_id       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sale_datetime TIMESTAMP     NOT NULL,
    total_amount  DECIMAL(12,2) NOT NULL
);

-- Таблица позиций в чеке
CREATE TABLE IF NOT EXISTS sale_item (
    sale_item_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sale_id         INT           NOT NULL REFERENCES sale(sale_id),
    product_id      INT           NOT NULL REFERENCES product(product_id),
    quantity        INT           NOT NULL,
    unit_sale_price DECIMAL(10,2) NOT NULL,
    unit_cost_price DECIMAL(10,2) NOT NULL
);

-- Индексы для ускорения соединений по внешним ключам (опционально)
CREATE INDEX IF NOT EXISTS idx_product_category ON product(category_id);
CREATE INDEX IF NOT EXISTS idx_supply_supplier ON supply(supplier_id);
CREATE INDEX IF NOT EXISTS idx_supply_item_supply ON supply_item(supply_id);
CREATE INDEX IF NOT EXISTS idx_supply_item_product ON supply_item(product_id);
CREATE INDEX IF NOT EXISTS idx_sale_item_sale ON sale_item(sale_id);
CREATE INDEX IF NOT EXISTS idx_sale_item_product ON sale_item(product_id);