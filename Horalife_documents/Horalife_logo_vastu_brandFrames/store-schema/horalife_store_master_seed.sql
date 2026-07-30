BEGIN;

-- =========================================================
-- HORA LIFE STORE - REALISTIC STARTER MASTER DATA
-- Notes:
-- 1. Product names and catalogue attributes are realistic.
-- 2. Prices, stock, image paths, origin and certificate details are starter values.
-- 3. Replace operational values with verified supplier data before production.
-- 4. Gemstone recommendations must remain subject to Kundali suitability rules.
-- =========================================================

-- ---------------------------------------------------------
-- 1. CATEGORIES
-- ---------------------------------------------------------

INSERT INTO store.categories
(
    name,
    slug,
    description,
    display_order,
    is_active
)
VALUES
    (
        'Gemstones',
        'gemstones',
        'Certified loose gemstones intended for astrology-related use after suitability review.',
        1,
        TRUE
    ),
    (
        'Yantras',
        'yantras',
        'Traditional yantras available in copper and other verified materials.',
        2,
        TRUE
    ),
    (
        'Rudraksha',
        'rudraksha',
        'Rudraksha beads and malas with supplier and authenticity details.',
        3,
        TRUE
    ),
    (
        'Puja and Remedy Items',
        'puja-remedy-items',
        'Supporting items used for traditional puja and remedy practices.',
        4,
        TRUE
    ),
    (
        'Remedy Kits',
        'remedy-kits',
        'Curated remedy kits containing clearly listed individual items.',
        5,
        TRUE
    )
ON CONFLICT (slug) DO UPDATE
SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    display_order = EXCLUDED.display_order,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- ---------------------------------------------------------
-- 2. REMEDY KEYS
-- ---------------------------------------------------------

INSERT INTO store.remedy_keys
(
    remedy_key,
    name,
    remedy_type,
    description,
    is_active
)
VALUES
    ('sun_support',       'Sun-related Support',       'planetary', 'Products mapped to a reviewed Sun-related recommendation.', TRUE),
    ('moon_support',      'Moon-related Support',      'planetary', 'Products mapped to a reviewed Moon-related recommendation.', TRUE),
    ('mars_support',      'Mars-related Support',      'planetary', 'Products mapped to a reviewed Mars-related recommendation.', TRUE),
    ('mercury_support',   'Mercury-related Support',   'planetary', 'Products mapped to a reviewed Mercury-related recommendation.', TRUE),
    ('jupiter_support',   'Jupiter-related Support',   'planetary', 'Products mapped to a reviewed Jupiter-related recommendation.', TRUE),
    ('venus_support',     'Venus-related Support',     'planetary', 'Products mapped to a reviewed Venus-related recommendation.', TRUE),
    ('saturn_support',    'Saturn-related Support',    'planetary', 'Products mapped to a reviewed Saturn-related recommendation.', TRUE),
    ('rahu_support',      'Rahu-related Support',      'planetary', 'Products mapped to a reviewed Rahu-related recommendation.', TRUE),
    ('ketu_support',      'Ketu-related Support',      'planetary', 'Products mapped to a reviewed Ketu-related recommendation.', TRUE),
    ('navagraha_balance', 'Navagraha Support',         'general',   'Products intended for general Navagraha worship or support.', TRUE),
    ('spiritual_practice','General Spiritual Practice','general',   'Products intended for general spiritual practice and puja.', TRUE)
ON CONFLICT (remedy_key) DO UPDATE
SET
    name = EXCLUDED.name,
    remedy_type = EXCLUDED.remedy_type,
    description = EXCLUDED.description,
    is_active = EXCLUDED.is_active;

-- ---------------------------------------------------------
-- 3. PRODUCTS
-- ---------------------------------------------------------

WITH product_seed
(
    category_slug,
    sku,
    slug,
    name,
    short_description,
    description,
    product_type,
    base_price,
    attributes,
    requires_consultation,
    is_featured,
    is_active
)
AS
(
    VALUES
    (
        'gemstones',
        'HL-GEM-RUBY',
        'ruby-manikya',
        'Ruby (Manikya)',
        'Ruby gemstone listing for reviewed Sun-related recommendations.',
        'Loose ruby gemstone. Weight, treatment, origin and laboratory certificate must be recorded at variant level before sale.',
        'gemstone',
        5500.00,
        '{"traditionalName":"Manikya","primaryColor":"Red","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        TRUE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-PEARL',
        'pearl-moti',
        'Pearl (Moti)',
        'Pearl listing for reviewed Moon-related recommendations.',
        'Loose pearl product. Natural or cultured classification, shape, lustre and certificate details must be disclosed per variant.',
        'gemstone',
        2500.00,
        '{"traditionalName":"Moti","primaryColor":"White","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        FALSE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-CORAL',
        'red-coral-moonga',
        'Red Coral (Moonga)',
        'Red coral listing for reviewed Mars-related recommendations.',
        'Loose red coral product. Origin, treatment, shape and laboratory verification must be recorded before sale.',
        'gemstone',
        4200.00,
        '{"traditionalName":"Moonga","primaryColor":"Red","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        FALSE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-EMERALD',
        'emerald-panna',
        'Emerald (Panna)',
        'Emerald gemstone listing for reviewed Mercury-related recommendations.',
        'Loose emerald gemstone. Treatment, clarity, origin and laboratory certificate must be captured per variant.',
        'gemstone',
        6000.00,
        '{"traditionalName":"Panna","primaryColor":"Green","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        TRUE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-YELLOW-SAPPHIRE',
        'yellow-sapphire-pukhraj',
        'Yellow Sapphire (Pukhraj)',
        'Yellow sapphire listing for reviewed Jupiter-related recommendations.',
        'Loose yellow sapphire gemstone. Treatment, origin, weight and laboratory certificate must be recorded per variant.',
        'gemstone',
        8500.00,
        '{"traditionalName":"Pukhraj","primaryColor":"Yellow","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        TRUE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-WHITE-SAPPHIRE',
        'white-sapphire-safed-pukhraj',
        'White Sapphire (Safed Pukhraj)',
        'White sapphire listing for reviewed Venus-related recommendations.',
        'Loose white sapphire gemstone. Transparency, treatment and laboratory certificate must be captured per variant.',
        'gemstone',
        7000.00,
        '{"traditionalName":"Safed Pukhraj","primaryColor":"White","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        FALSE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-BLUE-SAPPHIRE',
        'blue-sapphire-neelam',
        'Blue Sapphire (Neelam)',
        'Blue sapphire listing requiring strict suitability review.',
        'Loose blue sapphire gemstone. Do not surface as an automatic purchase recommendation without explicit rule-engine suitability and review.',
        'gemstone',
        9500.00,
        '{"traditionalName":"Neelam","primaryColor":"Blue","certificateRequired":true,"strictSuitabilityReview":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        TRUE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-HESSONITE',
        'hessonite-gomed',
        'Hessonite (Gomed)',
        'Hessonite listing for reviewed Rahu-related recommendations.',
        'Loose hessonite gemstone. Colour, clarity, treatment and laboratory verification must be captured per variant.',
        'gemstone',
        3800.00,
        '{"traditionalName":"Gomed","primaryColor":"Honey Brown","certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        FALSE,
        TRUE
    ),
    (
        'gemstones',
        'HL-GEM-CATS-EYE',
        'cats-eye-lehsunia',
        'Cat''s Eye (Lehsunia)',
        'Cat''s eye gemstone listing requiring suitability review.',
        'Loose cat''s eye gemstone. Chatoyancy, treatment and laboratory verification must be documented per variant.',
        'gemstone',
        4500.00,
        '{"traditionalName":"Lehsunia","primaryColor":"Greenish Yellow","certificateRequired":true,"strictSuitabilityReview":true,"supplierVerificationRequired":true}'::JSONB,
        TRUE,
        FALSE,
        TRUE
    ),
    (
        'yantras',
        'HL-YAN-NAVAGRAHA',
        'navagraha-yantra',
        'Navagraha Yantra',
        'Traditional Navagraha yantra for puja and spiritual practice.',
        'Navagraha yantra product. Material, dimensions, engraving process and energisation status must be clearly disclosed.',
        'yantra',
        1250.00,
        '{"material":"Copper","energised":false,"countryOfOrigin":"India"}'::JSONB,
        FALSE,
        TRUE,
        TRUE
    ),
    (
        'yantras',
        'HL-YAN-SHREE',
        'shree-yantra',
        'Shree Yantra',
        'Traditional Shree Yantra for puja and spiritual practice.',
        'Shree Yantra product. Material, dimensions, engraving process and energisation status must be clearly disclosed.',
        'yantra',
        1450.00,
        '{"material":"Copper","energised":false,"countryOfOrigin":"India"}'::JSONB,
        FALSE,
        TRUE,
        TRUE
    ),
    (
        'rudraksha',
        'HL-RUD-5M',
        'five-mukhi-rudraksha',
        '5 Mukhi Rudraksha',
        'Single 5 Mukhi Rudraksha bead with supplier verification.',
        'Individual 5 Mukhi Rudraksha bead. Origin, bead size, drilling and authenticity evidence must be recorded per variant.',
        'rudraksha',
        499.00,
        '{"mukhi":5,"certificateRequired":true,"supplierVerificationRequired":true}'::JSONB,
        FALSE,
        TRUE,
        TRUE
    )
)
INSERT INTO store.products
(
    category_id,
    sku,
    slug,
    name,
    short_description,
    description,
    product_type,
    base_price,
    currency,
    attributes,
    requires_consultation,
    is_featured,
    is_active
)
SELECT
    c.id,
    ps.sku,
    ps.slug,
    ps.name,
    ps.short_description,
    ps.description,
    ps.product_type,
    ps.base_price,
    'INR',
    ps.attributes,
    ps.requires_consultation,
    ps.is_featured,
    ps.is_active
FROM product_seed ps
JOIN store.categories c
    ON c.slug = ps.category_slug
ON CONFLICT (slug) DO UPDATE
SET
    category_id = EXCLUDED.category_id,
    sku = EXCLUDED.sku,
    name = EXCLUDED.name,
    short_description = EXCLUDED.short_description,
    description = EXCLUDED.description,
    product_type = EXCLUDED.product_type,
    base_price = EXCLUDED.base_price,
    currency = EXCLUDED.currency,
    attributes = EXCLUDED.attributes,
    requires_consultation = EXCLUDED.requires_consultation,
    is_featured = EXCLUDED.is_featured,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- ---------------------------------------------------------
-- 4. PRODUCT VARIANTS
-- Gemstones should eventually use one variant/lot per
-- verified physical stone or certificate number.
-- ---------------------------------------------------------

WITH variant_seed
(
    product_slug,
    sku,
    name,
    attributes,
    price,
    weight_grams,
    is_active
)
AS
(
    VALUES
    ('ruby-manikya',                  'HL-GEM-RUBY-5CT',     '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 5500.00, NULL, TRUE),
    ('pearl-moti',                    'HL-GEM-PEARL-5CT',    '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 2500.00, NULL, TRUE),
    ('red-coral-moonga',              'HL-GEM-CORAL-5CT',    '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 4200.00, NULL, TRUE),
    ('emerald-panna',                 'HL-GEM-EMERALD-5CT',  '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 6000.00, NULL, TRUE),
    ('yellow-sapphire-pukhraj',       'HL-GEM-YSAP-5CT',     '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 8500.00, NULL, TRUE),
    ('white-sapphire-safed-pukhraj',  'HL-GEM-WSAP-5CT',     '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 7000.00, NULL, TRUE),
    ('blue-sapphire-neelam',          'HL-GEM-BSAP-5CT',     '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 9500.00, NULL, TRUE),
    ('hessonite-gomed',               'HL-GEM-GOMED-5CT',    '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 3800.00, NULL, TRUE),
    ('cats-eye-lehsunia',             'HL-GEM-CATEYE-5CT',   '5.00–5.99 carat', '{"weightRangeCarat":"5.00-5.99","certificateStatus":"Pending supplier upload"}'::JSONB, 4500.00, NULL, TRUE),

    ('navagraha-yantra',              'HL-YAN-NAVA-CU-3',    'Copper, 3 inch',  '{"material":"Copper","sizeInches":3,"energised":false}'::JSONB, 1250.00, 120.000, TRUE),
    ('shree-yantra',                  'HL-YAN-SHREE-CU-3',   'Copper, 3 inch',  '{"material":"Copper","sizeInches":3,"energised":false}'::JSONB, 1450.00, 120.000, TRUE),
    ('five-mukhi-rudraksha',          'HL-RUD-5M-STD',       'Standard bead',   '{"mukhi":5,"beadSizeMm":"14-18","certificateStatus":"Pending supplier upload"}'::JSONB, 499.00, 5.000, TRUE)
)
INSERT INTO store.product_variants
(
    product_id,
    sku,
    name,
    attributes,
    price,
    weight_grams,
    is_active
)
SELECT
    p.id,
    vs.sku,
    vs.name,
    vs.attributes,
    vs.price,
    vs.weight_grams,
    vs.is_active
FROM variant_seed vs
JOIN store.products p
    ON p.slug = vs.product_slug
ON CONFLICT (sku) DO UPDATE
SET
    product_id = EXCLUDED.product_id,
    name = EXCLUDED.name,
    attributes = EXCLUDED.attributes,
    price = EXCLUDED.price,
    weight_grams = EXCLUDED.weight_grams,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- ---------------------------------------------------------
-- 5. PRODUCT IMAGES
-- Replace these local paths with Blob/S3/CDN URLs later.
-- ---------------------------------------------------------

WITH image_seed(product_slug, image_url, alt_text, display_order, is_primary)
AS
(
    VALUES
    ('ruby-manikya',                 '/assets/store/products/ruby-manikya.webp',                 'Ruby Manikya gemstone',                  1, TRUE),
    ('pearl-moti',                   '/assets/store/products/pearl-moti.webp',                   'Pearl Moti gemstone',                    1, TRUE),
    ('red-coral-moonga',             '/assets/store/products/red-coral-moonga.webp',             'Red Coral Moonga gemstone',              1, TRUE),
    ('emerald-panna',                '/assets/store/products/emerald-panna.webp',                'Emerald Panna gemstone',                 1, TRUE),
    ('yellow-sapphire-pukhraj',      '/assets/store/products/yellow-sapphire-pukhraj.webp',      'Yellow Sapphire Pukhraj gemstone',       1, TRUE),
    ('white-sapphire-safed-pukhraj', '/assets/store/products/white-sapphire.webp',               'White Sapphire gemstone',                1, TRUE),
    ('blue-sapphire-neelam',         '/assets/store/products/blue-sapphire-neelam.webp',         'Blue Sapphire Neelam gemstone',          1, TRUE),
    ('hessonite-gomed',              '/assets/store/products/hessonite-gomed.webp',              'Hessonite Gomed gemstone',               1, TRUE),
    ('cats-eye-lehsunia',            '/assets/store/products/cats-eye-lehsunia.webp',            'Cats Eye Lehsunia gemstone',             1, TRUE),
    ('navagraha-yantra',             '/assets/store/products/navagraha-yantra.webp',             'Copper Navagraha Yantra',                1, TRUE),
    ('shree-yantra',                 '/assets/store/products/shree-yantra.webp',                 'Copper Shree Yantra',                    1, TRUE),
    ('five-mukhi-rudraksha',         '/assets/store/products/five-mukhi-rudraksha.webp',         'Five Mukhi Rudraksha bead',              1, TRUE)
)
INSERT INTO store.product_images
(
    product_id,
    image_url,
    alt_text,
    display_order,
    is_primary
)
SELECT
    p.id,
    i.image_url,
    i.alt_text,
    i.display_order,
    i.is_primary
FROM image_seed i
JOIN store.products p
    ON p.slug = i.product_slug
WHERE NOT EXISTS
(
    SELECT 1
    FROM store.product_images pi
    WHERE pi.product_id = p.id
      AND pi.image_url = i.image_url
);

-- ---------------------------------------------------------
-- 6. INVENTORY
-- Starter stock only. Replace with supplier/warehouse data.
-- ---------------------------------------------------------

INSERT INTO store.inventory
(
    product_id,
    variant_id,
    stock_quantity,
    reserved_quantity,
    reorder_level
)
SELECT
    p.id,
    pv.id,
    CASE
        WHEN p.product_type = 'gemstone' THEN 5
        WHEN p.product_type = 'yantra' THEN 15
        WHEN p.product_type = 'rudraksha' THEN 20
        ELSE 10
    END,
    0,
    CASE
        WHEN p.product_type = 'gemstone' THEN 1
        ELSE 3
    END
FROM store.product_variants pv
JOIN store.products p
    ON p.id = pv.product_id
WHERE NOT EXISTS
(
    SELECT 1
    FROM store.inventory i
    WHERE i.product_id = p.id
      AND i.variant_id = pv.id
);

-- ---------------------------------------------------------
-- 7. PRODUCT-REMEDY MAPPINGS
-- These mappings only identify candidate products.
-- The Kundali engine must decide whether recommendation is valid.
-- ---------------------------------------------------------

WITH mapping_seed(product_slug, remedy_key, recommendation_rank, match_strength, notes)
AS
(
    VALUES
    ('ruby-manikya',                 'sun_support',        10, 90.00, 'Candidate product only; Kundali suitability must be confirmed.'),
    ('pearl-moti',                   'moon_support',       10, 90.00, 'Candidate product only; Kundali suitability must be confirmed.'),
    ('red-coral-moonga',             'mars_support',       10, 90.00, 'Candidate product only; Kundali suitability must be confirmed.'),
    ('emerald-panna',                'mercury_support',    10, 90.00, 'Candidate product only; Kundali suitability must be confirmed.'),
    ('yellow-sapphire-pukhraj',      'jupiter_support',    10, 90.00, 'Candidate product only; Kundali suitability must be confirmed.'),
    ('white-sapphire-safed-pukhraj', 'venus_support',      10, 85.00, 'Candidate product only; Kundali suitability must be confirmed.'),
    ('blue-sapphire-neelam',         'saturn_support',     10, 80.00, 'Strict suitability review required before recommendation.'),
    ('hessonite-gomed',              'rahu_support',       10, 80.00, 'Strict suitability review required before recommendation.'),
    ('cats-eye-lehsunia',            'ketu_support',       10, 80.00, 'Strict suitability review required before recommendation.'),
    ('navagraha-yantra',             'navagraha_balance',  20, 75.00, 'General candidate product; do not present as guaranteed outcome.'),
    ('shree-yantra',                 'spiritual_practice', 20, 70.00, 'General spiritual-practice product.'),
    ('five-mukhi-rudraksha',         'spiritual_practice', 30, 65.00, 'General spiritual-practice product.')
)
INSERT INTO store.product_remedy_mappings
(
    product_id,
    remedy_key_id,
    recommendation_rank,
    match_strength,
    notes
)
SELECT
    p.id,
    rk.id,
    ms.recommendation_rank,
    ms.match_strength,
    ms.notes
FROM mapping_seed ms
JOIN store.products p
    ON p.slug = ms.product_slug
JOIN store.remedy_keys rk
    ON rk.remedy_key = ms.remedy_key
ON CONFLICT (product_id, remedy_key_id) DO UPDATE
SET
    recommendation_rank = EXCLUDED.recommendation_rank,
    match_strength = EXCLUDED.match_strength,
    notes = EXCLUDED.notes;

COMMIT;
