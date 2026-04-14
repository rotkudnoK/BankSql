SQL-запрос для верификации
```
WITH account_stats AS (
	    SELECT 
	        client_id, 
	        COUNT(*) AS account_count, 
	        SUM(balance) AS total_balance
	    FROM accounts
	    GROUP BY client_id
	),
	transfer_stats AS (
	    SELECT 
	        a.client_id, 
	        COUNT(CASE WHEN t.status = 'completed' THEN t.id END) AS completed_transfer_count
	    FROM accounts a
	    LEFT JOIN transfers t ON a.id = t.from_account_id
	    GROUP BY a.client_id
	)
	SELECT 
	    c.full_name,
	    c.status,
	    ac.account_count,
	    ac.total_balance,
	    tr.completed_transfer_count
	FROM clients c
	LEFT JOIN account_stats ac ON c.id = ac.client_id
	LEFT JOIN transfer_stats tr ON c.id = tr.client_id
	ORDER BY total_balance DESC;
```
