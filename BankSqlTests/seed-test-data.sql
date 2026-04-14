WITH new_clients AS (
	INSERT INTO clients (full_name, inn, status)
	VALUES
		('Ivan Ivanov', '775598987121', 'active'),
		('Fedor Fedorov', '674568987857', 'blocked'),
		('Pavel Pavlovich', '947329758409', 'closed')
	RETURNING id, inn
),
new_accounts AS (
	INSERT INTO accounts (client_id, account_number, currency, balance)
	SELECT id, '15462315453215345321', 'RUB', 1000.00 FROM new_clients WHERE inn = '775598987121' UNION ALL
	SELECT id, '26532145895655896542', 'USD', 1000.00 FROM new_clients WHERE inn = '775598987121' UNION ALL
	SELECT id, '54563215489652154123', 'RUB', 1000.00 FROM new_clients WHERE inn = '674568987857' UNION ALL
	SELECT id, '25451236542548985452', 'USD', 1000.00 FROM new_clients WHERE inn = '674568987857' UNION ALL
	SELECT id, '36541231685464324231', 'RUB', 1000.00 FROM new_clients WHERE inn = '947329758409' UNION ALL
	SELECT id, '97846544132413534121', 'USD', 1000.00 FROM new_clients WHERE inn = '947329758409'
	RETURNING id, account_number
)
INSERT INTO transfers (from_account_id, to_account_id, amount, currency, status)
SELECT from_a.id, to_a.id, 50.00, 'RUB', 'completed' FROM new_accounts from_a, new_accounts to_a
WHERE from_a.account_number = '15462315453215345321' AND to_a.account_number = '54563215489652154123' UNION ALL
SELECT from_a.id, to_a.id, 50.00, 'USD', 'completed' FROM new_accounts from_a, new_accounts to_a
WHERE from_a.account_number = '26532145895655896542' AND to_a.account_number = '25451236542548985452' UNION ALL
SELECT from_a.id, to_a.id, 50.00, 'RUB', 'pending' FROM new_accounts from_a, new_accounts to_a
WHERE from_a.account_number = '15462315453215345321' AND to_a.account_number = '54563215489652154123' UNION ALL
SELECT from_a.id, to_a.id, 50.00, 'USD', 'pending' FROM new_accounts from_a, new_accounts to_a
WHERE from_a.account_number = '26532145895655896542' AND to_a.account_number = '25451236542548985452' UNION ALL
SELECT from_a.id, to_a.id, 50.00, 'RUB', 'rejected' FROM new_accounts from_a, new_accounts to_a
WHERE from_a.account_number = '15462315453215345321' AND to_a.account_number = '54563215489652154123';